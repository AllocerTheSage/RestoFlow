using Business.Abstracts;
using Business.DTOs.OrderDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Concretes
{
    // OrderManager: Sistemin "Sipariş Beyni"dir.
    // Garsonun girdiği siparişleri denetler, fiyatları doğrular ve mutfağa iletir.
    public class OrderManager : IOrderService
    {
        private readonly IGenericRepository<Order> _orderRepository; // Adisyonları (Masaları) yönetmek için
        private readonly IGenericRepository<Product> _productRepository; // Ürün fiyatlarını ve stok/durum bilgisini çekmek için
        private readonly IUnitOfWork _unitOfWork; // Yapılan tüm değişiklikleri tek seferde veritabanına kaydetmek (Commit) için
        private readonly ILogger<OrderManager> _logger; // Arka planda olan biteni (Hatalar, yeni siparişler) kayıt altına almak için

        // Dependency Injection (Bağımlılıkların dışarıdan alınması)
        public OrderManager(
            IGenericRepository<Order> orderRepository,
            IGenericRepository<Product> productRepository,
            IUnitOfWork unitOfWork,
            ILogger<OrderManager> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // Garsonun tabletten "Siparişi Onayla" dediği an tetiklenen metot.
        // waiterId parametresini dışarıdan (DTO'dan) değil, Controller içindeki güvenli Token'dan alıyoruz.
        public async Task<IResult> CreateOrderAsync(OrderCreateDto orderDto, string waiterId)
        {
            // 1. ADIM: YENİ ADİSYON TASLAĞI OLUŞTURMA
            // Henüz veritabanına kaydetmiyoruz, sadece C# hafızasında (RAM) adisyonu hazırlıyoruz.
            var order = new Order
            {
                // Guid.NewGuid() karmaşık bir şifre üretir (Örn: 550e8400-e29b-41d4-a716-446655440000).
                // Biz bunun sadece ilk 8 hanesini alarak göze hoş gelen bir fiş numarası yaratıyoruz.
                OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                TableNumber = orderDto.TableNumber,
                GuestCount = orderDto.GuestCount,
                CustomerName = orderDto.CustomerName,
                WaiterId = waiterId,

                // İlk yaratılan sipariş her zaman "Bekliyor" durumundadır. Mutfak bunu kendi ekranında yeni düşmüş olarak görür.
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            decimal totalPrice = 0;

            // 2. ADIM: SİPARİŞ İÇERİĞİNİ (ÜRÜNLERİ) TEK TEK İŞLEME VE GÜVENLİK KONTROLÜ
            foreach (var itemDto in orderDto.Items)
            {
                // KRİTİK GÜVENLİK DUVARI: Ürünün fiyatını garsonun gönderdiği paketten (DTO) ALMIYORUZ!
                // Kötü niyetli biri araya girip 500 TL'lik eti 1 TL olarak gönderebilir.
                // Bu yüzden ürünün ID'sine bakıp, en güncel ve gerçek fiyatını veritabanından kendimiz çekiyoruz.
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);

                // Eğer böyle bir ürün yoksa VEYA mutfak tarafından az önce "Satışa Kapatıldı" (IsActive = false) ise işlemi iptal et.
                if (product == null || !product.IsActive)
                {
                    return new ErrorResult($"İşlem durduruldu: {itemDto.ProductId} referanslı ürün bulunamadı veya şu an satışa kapalı!");
                }

                // Güvenlikten geçen ürünü adisyonun satırlarına (OrderItem) ekliyoruz.
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price, // Güvendiğimiz fiyat (Veritabanından gelen)
                    Note = itemDto.Note,       // Müşteri notu (Örn: "Soğansız")
                    IsComplimentary = false    // İlk siparişte hiçbir ürün varsayılan olarak "İkram" olamaz.
                };

                // O satırın toplam tutarını hesaplayıp genel adisyon tutarına ekliyoruz (Örn: 2 x 150 TL = 300 TL)
                totalPrice += (orderItem.UnitPrice * orderItem.Quantity);

                // Hazırlanan bu satırı, oluşturduğumuz taslak adisyona bağlıyoruz.
                order.OrderItems.Add(orderItem);
            }

            // 3. ADIM: TOPLAM FİYATI BELİRLEME
            order.TotalPrice = totalPrice;

            // 4. ADIM: VERİTABANINA KAYIT (COMMIT)
            // Hazırladığımız bu devasa adisyon paketini tek bir işlem (Transaction) olarak veritabanına yazıyoruz.
            await _orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 5. ADIM: SİSTEM GÜNLÜĞÜ (LOG) OLUŞTURMA
            // Patron gün sonunda veya bir hata olduğunda "Bu siparişi kim, ne zaman, kaç paraya girdi?" diye merak ederse buraya bakacak.
            _logger.LogInformation("YENİ SİPARİŞ! Adisyon: {OrderNo}, Masa: {Table}, Tutar: {Total} TL, Garson: {WaiterId}",
                order.OrderNumber, order.TableNumber, order.TotalPrice, waiterId);

            return new SuccessResult($"Sipariş başarıyla mutfağa iletildi. Fiş No: {order.OrderNumber}");

        }
        // Business/Concretes/OrderManager.cs içine eklenecek metotlar:

        // 1. ADIM: MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        // Neden? Çünkü mutfak personelinin ödenmiş veya iptal edilmiş eski siparişlerle kafasını karıştırmıyoruz.
        // Business/Concretes/OrderManager.cs içindeki GetPendingOrdersAsync metodu:

        public async Task<IDataResult<List<Order>>> GetPendingOrdersAsync()
        {
            // 1. GetAll(): Veritabanındaki 'Orders' (Adisyonlar) tablosuna bir kapı açıyoruz.
            // Henüz verileri çekmedik, sadece ne çekeceğimizi tarif etmeye başlıyoruz.
            var orders = await _orderRepository.GetAll()

                // 2. .Where(): Filtreleme yapıyoruz. Mutfak personeli sadece "Bekleyen" (Pending)
                // veya "Hazırlanıyor" (Preparing) olan siparişleri görmeli.
                // Tamamlanmış veya iptal edilmiş siparişleri buraya getirip mutfağı kalabalıklaştırmıyoruz.
                .Where(x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Preparing)

                // 3. .Include(): Eager Loading (Hevesli Yükleme) yapıyoruz.
                // Veritabanında 'Order' tablosu ile 'OrderItem' tablosu ayrıdır. 
                // Eğer bunu yazmazsak, siparişi çekeriz ama içindeki ürün listesi BOŞ gelir.
                // "Adisyonu getirirken, o adisyona bağlı olan tüm sipariş satırlarını da kolunun altına al getir" diyoruz.
                .Include(x => x.OrderItems)

                // 4. .ThenInclude(): İlişkinin ilişkisine gidiyoruz.
                // OrderItem tablosunda sadece 'ProductId' vardır (örn: 5). 
                // Mutfaktaki aşçı "5 numara" değil, "Hamburger" yazısını görmek ister.
                // "Getirdiğin o sipariş satırlarının (OrderItem) içindeki ProductId'yi kullan ve 
                // o ürünün isminin, fiyatının olduğu 'Product' tablosuna da uğrayıp o bilgileri de getir" diyoruz.
                .ThenInclude(oi => oi.Product)

                // 5. .ToListAsync(): İşte şimdi tetiği çekiyoruz!
                // Yukarıda yazdığımız tüm bu kurallar birleştirilip tek bir SQL sorgusuna dönüştürülür,
                // veritabanına gönderilir ve sonuçlar bir liste halinde RAM'e (hafızaya) alınır.
                .ToListAsync();

            return new SuccessDataResult<List<Order>>(orders, "Mutfak ekranı verileri başarıyla hazırlandı.");
        }
        // Siparişi "Hazır" (Ready) durumuna getirir ve stoğu otomatik düşer.
        public async Task<IResult> SetOrderReadyAsync(int orderId)
        {
            // 1. ADIM: Siparişi ve içindeki ürünleri (Items) buluyoruz.
            // 'Include' kullanıyoruz çünkü siparişin içindeki her bir satıra (Hamburger mi, Kola mı?) ihtiyacımız var.
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            // Güvenlik Kontrolü: Sipariş yoksa hata döndür.
            if (order == null)
            {
                return new ErrorResult("Sipariş veritabanında bulunamadı!");
            }

            // Güvenlik Kontrolü 2: Sipariş zaten hazırsa, stokları tekrar tekrar düşmemek için işlemi durdur.
            if (order.Status == OrderStatus.Ready)
            {
                return new ErrorResult("Bu sipariş zaten hazırlandı, stoklar daha önce düşüldü.");
            }

            // 2. ADIM: STOK DÜŞME DÖNGÜSÜ
            // Siparişin içindeki ürünleri tek tek geziyoruz.
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    // Örnek: Mutfaktaki 50 hamburger ekmeğinden, sipariş edilen 2 tanesini çıkarıyoruz.
                    product.StockQuantity -= item.Quantity;

                    // Ürün tablosundaki yeni stok miktarını güncelliyoruz.
                    _productRepository.Update(product);
                }
            }

            // 3. ADIM: DURUM GÜNCELLEME
            // Adisyonun durumunu artık 'Hazır' olarak işaretliyoruz.
            order.Status = OrderStatus.Ready;
            _orderRepository.Update(order);

            // 4. ADIM: KAYDETME (UNIT OF WORK)
            // Yukarıdaki tüm stok düşme ve durum güncelleme işlemlerini TEK BİR SQL sorgusuyla
            // veritabanına kalıcı olarak yazıyoruz.
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult($"Sipariş {order.OrderNumber} başarıyla hazırlandı ve stoklar güncellendi.");
        }
        public async Task<IResult> CloseOrderAsync(int orderId)
        {
            // 1. Kapatılacak siparişi buluyoruz.
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                return new ErrorResult("Kapatılacak sipariş bulunamadı!");
            }

            // Güvenlik: Sadece 'Hazır' (3) veya 'Teslim Edildi' (4) olan siparişler kapatılabilir.
            // Bekleyen veya iptal edilmiş bir siparişin ödemesi alınamaz.
            if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Delivered)
            {
                return new ErrorResult("Bu sipariş henüz ödeme almak için uygun durumda değil.");
            }

            // 2. Siparişin durumunu "Completed" (5) yapıyoruz.
            order.Status = OrderStatus.Completed;

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult($"Sipariş {order.OrderNumber} başarıyla kapatıldı. {order.TableNumber} Ödemesi alındı.");
        }
        // Patronun gün sonu raporu: Bugün kasaya giren toplam net para.
        public async Task<IDataResult<decimal>> GetDailyRevenueAsync()
        {
            // 1. ZAMAN FİLTRESİ: Bugünün tarihini saat 00:00:00 olarak alırız.
            var today = DateTime.Today;

            // 2. VERİ ÇEKME: Arşive inip "Bana bugünden sonra açılmış ve ÖDENMİŞ adisyonları getir" diyoruz.
            // Yine ürünlerin fiyatını görebilmek için Include ile Product tablosuna kadar iniyoruz.
            // 1. VERİ ÇEKME İŞLEMİ: Arşive inip "Bana bugünden sonra açılmış ve ÖDENMİŞ adisyonları getir" diyoruz.
            var todayCompletedOrders = await _orderRepository.GetAll()

                // .Where(): Veritabanındaki binlerce siparişi filtreler.
                // Şart 1: Status == OrderStatus.Completed (Sadece hesabı ödenip kapanmış masalar)
                // Şart 2: CreatedDate >= today (Sadece gece 00:00'dan sonra, yani bugün açılan adisyonlar)
                .Where(x => x.Status == OrderStatus.Completed && x.CreatedDate >= today)

                // .Include(): Adisyon kağıdını bulduk ama içi boş gelmesin diye, 
                // o adisyona ait sipariş kalemlerini (OrderItems) de veritabanından çekip içine koyuyoruz.
                .Include(x => x.OrderItems)

                    // .ThenInclude(): Sadece sipariş kalemlerini çekmek yetmez. 
                    // Bize o kalemlerin içindeki ürünün fiyatı (Price) lazım. 
                    // Bu yüzden OrderItem'dan Product (Ürün) tablosuna geçiş yapıp ürünün detaylarını da getiriyoruz.
                    .ThenInclude(oi => oi.Product)

                // .ToListAsync(): Yukarıda yazdığımız tüm bu kuralları (Filtreler ve Includelar)
                // tek bir SQL sorgusuna çevirip veritabanına gönderir ve sonucu bize bir Liste olarak geri döndürür.
                .ToListAsync();

            // 3. MATEMATİK KISMI: Başlangıçta kasamızda 0 TL var.
            decimal totalRevenue = 0;

            // Her bir ödenmiş adisyonu tek tek açıp bakıyoruz...
            foreach (var order in todayCompletedOrders)
            {
                // O adisyonun içindeki her bir satıra (Hamburger, Kola vs.) bakıyoruz...
                foreach (var item in order.OrderItems)
                {
                    // Kasadaki paraya = (Satılan Adet * Ürünün Fiyatı) ekliyoruz.
                    totalRevenue += item.Quantity * item.Product.Price;
                }
            }

            // 4. SONUÇ: Patronun önüne net rakamı koyuyoruz.
            return new SuccessDataResult<decimal>(totalRevenue, "Günlük ciro başarıyla hesaplandı.");
        }
        public async Task<IResult> CancelOrderAsync(int orderId, string cancellationReason)
        {
            // 1. Siparişi, içindeki ürünler ve ürünlerin detaylarıyla (IsReturnable, Stock vb.) birlikte getir.
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            // Güvenlik 1: Sipariş yok mu?
            if (order == null)
            {
                return new ErrorResult("İptal edilecek sipariş bulunamadı!");
            }

            // Güvenlik 2: Zaten kapanmış veya daha önce iptal edilmiş bir sipariş iptal edilemez.
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Hesabı ödenmiş veya zaten iptal edilmiş bir siparişe işlem yapılamaz.");
            }

            // 3. AKILLI İADE MANTIĞI (Senin Tasarımın)
            // Eğer sipariş mutfakta hazırlandıysa veya masaya gittiyse (Yani stoklar DÜŞTÜYSE)
            if (order.Status == OrderStatus.Ready || order.Status == OrderStatus.Delivered)
            {
                foreach (var item in order.OrderItems)
                {
                    // Eğer ürün iade edilebilir bir ürünse (Örn: Kola), dolaba geri koy
                    if (item.Product.IsReturnable == true)
                    {
                        item.Product.StockQuantity += item.Quantity; // Stoğu geri artır
                        _productRepository.Update(item.Product);
                    }
                    // Else yazmamıza gerek yok. IsReturnable = false ise (Hamburger), 
                    // stoğa dokunmuyoruz. Stok azaldığıyla kalıyor ve ürün zayi oluyor.
                }
            }
            // NOT: Eğer sipariş "Pending" ise yukarıdaki "if" bloğuna hiç girmez. 
            // Stok düşmediği için iade edecek bir şey de yoktur.

            // 4. İptal Sebebini Yaz ve Durumu Güncelle
            order.CancellationReason = cancellationReason;
            order.Status = OrderStatus.Canceled;

            _orderRepository.Update(order);

            // 5. Veritabanına Kaydet
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult($"{order.TableNumber}'in {order.OrderNumber} fişli siparişi başarıyla iptal edildi. Sebep: {cancellationReason}");
        }
    }
}