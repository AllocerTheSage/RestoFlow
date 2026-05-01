using Business.Abstracts;
using Business.DTOs.OrderDtos;
using Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Token içinden garsonun ID'sini okumak için gerekli!

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu Controller'a Token'ı (Yaka Kartı) olmayan hiç kimse giremez.
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // [GARSON] Yeni sipariş girdiği uç.
        // Kapıdaki asma kilit: Sadece "CreateOrder" yetkisi olanlar girebilir.
        [HttpPost("create")]
        [Authorize(Policy = Permissions.Operations.CreateOrder)]
        public async Task<IActionResult> CreateOrder(OrderCreateDto orderDto)
        {
            // İŞTE SİHİR BURADA: Token'ın içinden garsonun gerçek ID'sini (NameIdentifier) söküp alıyoruz.
            // Bu sayede garson DTO'ya sahte bir ID yazsa bile sistem onu umursamaz.
            var waiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Güvenlik kontrolü: Eğer bir şekilde Token'dan ID okunamadıysa işlemi durdur.
            if (string.IsNullOrEmpty(waiterId))
            {
                return Unauthorized("Güvenlik İhlali: Kullanıcı kimliği doğrulanamadı.");
            }

            // DTO'yu ve güvenli şekilde aldığımız Garson ID'sini Manager'a (Sipariş Beynine) gönderiyoruz.
            var result = await _orderService.CreateOrderAsync(orderDto, waiterId);

            if (result.Success)
            {
                return Ok(result); // 200 Başarılı
            }

            return BadRequest(result); // 400 Hata (Örn: Ürün bulunamadı, ürün satışa kapalı vb.)
        }
        // 1. MUTFAK LİSTESİ: Sadece bekleyen siparişleri getirir.
        [HttpGet("pending")]
        [Authorize(Policy = Permissions.Operations.TrackOrderStatus)] // Sadece sipariş izleme yetkisi olanlar görebilir.
        public async Task<IActionResult> GetPendingOrders()
        {
            // Manager'a gidip "Bana mutfağın yapması gereken işleri getir" diyoruz.
            var result = await _orderService.GetPendingOrdersAsync();

            if (result.Success)
            {
                return Ok(result); // 200 döner ve siparişleri liste olarak sunar.
            }
            return BadRequest(result);
        }

        // 2. MUTFAK ONAYI: Aşçı "Tamam" dediğinde çalışır.
        [HttpPost("complete-preparation/{id}")]
        [Authorize(Policy = Permissions.Operations.ConfirmAndDeductStock)] // Stok düşme yetkisi olan (Aşçı/Admin) yapabilir.
        public async Task<IActionResult> CompletePreparation(int id)
        {
            // Manager'a gidip "ID'si şu olan sipariş pişti, stokları düş ve durumu hazır yap" diyoruz.
            var result = await _orderService.SetOrderReadyAsync(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        // ==========================================
        // 5. HESAP KAPATMA VE MASAYI BOŞALTMA UCU
        // ==========================================
        // Dışarıdan "/api/Orders/close/1" şeklinde bir istek geldiğinde çalışır.
        [HttpPost("close/{id}")]
        [Authorize] // Kasiyer/Admin yetki kontrolü (Şu an token'ı olan herkes yapabilir, ileride sadece kasaya özel kısıtlayacağız).
        public async Task<IActionResult> CloseOrder(int id)
        {
            // 1. ADIM: İŞİ YÖNETİCİYE DEVRETME
            // Kasiyerin gönderdiği Adisyon ID'sini alıp bizim akıllı yöneticimize (OrderManager) gönderiyoruz.
            // Yönetici gidip siparişi bulacak, "Completed" yapacak ve bağlı olduğu masayı "Empty" (Boş) yapacak.
            var result = await _orderService.CloseOrderAsync(id);

            // 2. ADIM: SONUÇ KONTROLÜ
            // Eğer işlem başarılıysa (Güvenlikten geçtiyse ve masa başarıyla boşaltıldıysa) 200 OK ve başarı mesajı dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Eğer sipariş bulunamadıysa VEYA henüz mutfakta hazırlanıyorsa (güvenlik duvarına takıldıysa) 400 Bad Request dön.
            return BadRequest(result);
        }
        // [PATRON EKRANI] Günlük net ciroyu getirir.
        // Neden HttpGet? Çünkü veritabanına yeni bir veri EKLEMİYORUZ (Post değil), sadece veri OKUYORUZ.
        [HttpGet("daily-revenue")]
        [Authorize(Policy = "Admin")] // Eğer sadece patron görsün istersen ileride buraya (Policy = "Admin") ekleyebiliriz.
        public async Task<IActionResult> GetDailyRevenue()
        {
            // Garson, aşçıya (OrderService) gidip ciroyu soruyor:
            var result = await _orderService.GetDailyRevenueAsync();

            // Eğer aşçı başarıyla hesapladıysa (Success == true), 200 OK koduyla parayı patrona sun.
            if (result.Success)
            {
                return Ok(result);
            }

            // Bir şeyler ters gittiyse (Hata varsa), 400 Bad Request ile hatayı söyle.
            return BadRequest(result);
        }
        // [KASİYER / YÖNETİCİ] Belirtilen siparişi bir sebeple iptal eder.
        [HttpPost("cancel/{id}")]
        [Authorize(Policy = Permissions.TableManagement.CancelOrder)]
        public async Task<IActionResult> CancelOrder(int id, [FromQuery] string reason)
        {
            // Garson, müşterinin iptal sebebini alıp mutfağa (OrderService) iletiyor.
            // İleride bu kısmı güvenlik (OWASP) standartlarına göre daha sıkı denetleyebiliriz, 
            // ama şimdilik doğrudan iletiyoruz.
            var result = await _orderService.CancelOrderAsync(id, reason);

            // Eğer iptal başarılıysa ve stoklar doğru ayarlandıysa 200 OK dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Sipariş zaten kapalıysa veya bulunamadıysa 400 Bad Request dön.
            return BadRequest(result);
        }
        // ==========================================
        // 4. ŞEF / PATRON EKRANI: İKRAM YAP (MÜESSESEDEN)
        // ==========================================
        // Neden HttpPatch? Çünkü veritabanına yeni bir sipariş EKLEMİYORUZ (Post değil),
        // Tüm siparişi GÜNCELLEMİYORUZ (Put değil). Sadece var olan bir siparişin 
        // içindeki küçücük bir detayı (IsComplimentary) değiştiriyoruz (Yama yapıyoruz = Patch).
        [HttpPatch("{orderId}/complimentary/{orderItemId}")]
        [Authorize] // İleride buraya Policy = "AdminOrChef" ekleyeceğiz. Her garson kafasına göre ikram yapamasın!
        public async Task<IActionResult> MakeItemComplimentary(int orderId, int orderItemId)
        {
            // Garson/Şef, hangi masanın (orderId) hangi ürününün (orderItemId) 
            // ikram edileceğini alıp mutfağa (OrderService) iletiyor.
            var result = await _orderService.MakeItemComplimentaryAsync(orderId, orderItemId);

            // Eğer mutfak "Tamam, fiyatı adisyondan düştüm" derse, kasiyere 200 OK ile sonucu göster.
            if (result.Success)
            {
                return Ok(result);
            }

            // Eğer bir şeyler ters giderse (Hesap kapanmışsa, ürün yoksa vb.) 400 Bad Request ile hatayı fırlat.
            return BadRequest(result);
        }
        // Mevcut ve açık olan bir siparişe (Adisyona) yeni ürünler ekler.
        [HttpPost("add-items")]
        [Authorize(Roles = "Admin,Waiter")] // Sadece Admin ve Garson masaya ürün ekleyebilir
        public async Task<IActionResult> AddItemsToOrder([FromBody] AddItemsToOrderDto addItemsDto)
        {
            // İşi Manager'a (Aşçıya) devrediyoruz.
            var result = await _orderService.AddItemsToOrderAsync(addItemsDto);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        // ==========================================
        // 8. KASA OPERASYONU: İNDİRİM UYGULAMA UCU
        // ==========================================
        // Kasiyer, adisyon kapanmadan önce belirli bir tutarda indirim yapmak istediğinde çalışır.
        // Dışarıdan "/api/Orders/apply-discount/1?amount=50" şeklinde çağrılır.
        [HttpPost("apply-discount/{id}")]
        [Authorize(Policy = Permissions.Finance.ApplyDiscount)] // Token'ındaki harika yetki sistemiyle tam uyumlu!
        public async Task<IActionResult> ApplyDiscount(int id, [FromQuery] decimal amount)
        {
            // Kasiyerin girdiği masa ID'sini ve indirim tutarını alıp bizim akıllı kasaya (OrderManager) gönderiyoruz.
            var result = await _orderService.ApplyDiscountAsync(id, amount);

            // Eğer işlem başarılıysa (Güvenlikten geçtiyse ve hesap eksiden düşmediyse) 200 OK dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Hesabı kapanmış bir masaya indirim yapılmaya çalışılırsa 400 Bad Request ile işlemi reddet.
            return BadRequest(result);
        }
        // ==========================================
        // 9. OPERASYON: MASA TAŞIMA (TABLE TRANSFER) UCU
        // ==========================================
        // Müşteri masa değiştirmek istediğinde garsonun tabletten tetikleyeceği uç.
        // Dışarıdan "POST /api/Orders/transfer-table" şeklinde, JSON kuryesiyle (DTO) çağrılır.
        [HttpPost("transfer-table")]
        [Authorize(Policy = Permissions.TableManagement.TransferOrder)] // Token'daki yetkinle tam uyumlu!
        public async Task<IActionResult> TransferTable([FromBody] TransferTableDto transferDto)
        {
            // Garsonun tabletten gönderdiği kuryeyi (DTO) alıp doğrudan yöneticiye (OrderManager) veriyoruz.
            var result = await _orderService.TransferTableAsync(transferDto);

            // Eğer taşıma başarılıysa (Hedef masa boşsa, adisyon açıksa vs.) 200 OK dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Hedef masa doluysa veya adisyon bulunamazsa 400 Bad Request ile işlemi reddet.
            return BadRequest(result);
        }
        // ==========================================
        // 10. OPERASYON: ADİSYONDAN ÜRÜN ÇIKARMA (SİLME) UCU
        // ==========================================
        // Garsonun yanlış girdiği veya müşterinin iptal ettiği tek bir ürünü adisyondan çıkarır.
        // Neden HttpDelete? Çünkü var olan bir veriyi (satırı) siliyoruz.
        // Dışarıdan "DELETE /api/Orders/1/remove-item/5" şeklinde çağrılır. (1: Adisyon ID, 5: Satır ID)
        [HttpDelete("{orderId}/remove-item/{orderItemId}")]
        [Authorize(Policy = Permissions.TableManagement.DeleteProduct)] // O muazzam asma kilidimiz devrede!
        public async Task<IActionResult> RemoveItemFromOrder(int orderId, int orderItemId)
        {
            // İşi akıllı yöneticimize (OrderManager) devrediyoruz.
            // O gidip stoğa bakacak, iade edilecekse edecek ve toplam fiyatı güncelleyecek.
            var result = await _orderService.RemoveItemFromOrderAsync(orderId, orderItemId);

            // Eğer işlem başarılıysa 200 OK dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Hesap zaten kapalıysa, iptal edildiyse veya ürün bulunamazsa 400 Bad Request dön.
            return BadRequest(result);
        }
        // 3. SÜTUN GEÇİŞİ: Mutfak "Hazırlamaya Başla" dediğinde çalışır.
        [HttpPost("start-preparation/{id}")]
        [Authorize(Policy = Permissions.Operations.ConfirmAndDeductStock)]
        public async Task<IActionResult> StartPreparation(int id)
        {
            var result = await _orderService.StartPreparationAsync(id);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
        [HttpGet("table/{tableId}/active")]
        [Authorize]
        public async Task<IActionResult> GetActiveOrder(int tableId)
        {
            var result = await _orderService.GetActiveOrderByTableIdAsync(tableId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result); // Masa boşsa buraya düşecek, sorun yok.
        }
    }
}