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
        // [KASİYER] Hesabı kapatır ve adisyonu tamamlar.
        [HttpPost("close/{id}")]
        [Authorize] // İleride buraya kasanın özel yetkisini (Policy) ekleyebiliriz.
        public async Task<IActionResult> CloseOrder(int id)
        {
            var result = await _orderService.CloseOrderAsync(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        // [PATRON EKRANI] Günlük net ciroyu getirir.
        // Neden HttpGet? Çünkü veritabanına yeni bir veri EKLEMİYORUZ (Post değil), sadece veri OKUYORUZ.
        [HttpGet("daily-revenue")]
        [Authorize] // Eğer sadece patron görsün istersen ileride buraya (Policy = "Admin") ekleyebiliriz.
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
        [Authorize]
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
    }
}