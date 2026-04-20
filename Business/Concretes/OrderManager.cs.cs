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
        private readonly IGenericRepository<OrderItem> _orderItemRepository;
        private readonly IGenericRepository<Table> _tableRepository; // MASA DURUMU İÇİN EKLENDİ (DOLU/BOŞ)
        private readonly IUnitOfWork _unitOfWork; // Yapılan tüm değişiklikleri tek seferde veritabanına kaydetmek (Commit) için
        private readonly ILogger<OrderManager> _logger; // Arka planda olan biteni (Hatalar, yeni siparişler) kayıt altına almak için

        // Dependency Injection (Bağımlılıkların dışarıdan alınması)
        public OrderManager(
            IGenericRepository<Order> orderRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<OrderItem> orderItemRepository,
            IGenericRepository<Table> tableRepository,
            IUnitOfWork unitOfWork,
            ILogger<OrderManager> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _orderItemRepository = orderItemRepository;
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // Garsonun tabletten "Siparişi Onayla" dediği an tetiklenen metot.
        // waiterId parametresini dışarıdan (DTO'dan) değil, Controller içindeki güvenli Token'dan alıyoruz.
        public async Task<IResult> CreateOrderAsync(OrderCreateDto orderDto, string waiterId)
        {
            // 1. ADIM: YENİ ADİSYON TASLAĞI OLUŞTURMA
            var order = new Order
            {
                OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),

                // ==========================================
                // [DEĞİŞTİ]: Eski TableNumber = orderDto.TableNumber satırı SİLİNDİ!
                // YENİ HALİ: Artık garsonun seçtiği masanın değişmez kimliğini (ID) kaydediyoruz.
                // ==========================================
                TableId = orderDto.TableId,

                GuestCount = orderDto.GuestCount,
                CustomerName = orderDto.CustomerName,
                WaiterId = waiterId,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            decimal totalPrice = 0;

            // 2. ADIM: SİPARİŞ İÇERİĞİNİ (ÜRÜNLERİ) TEK TEK İŞLEME VE GÜVENLİK KONTROLÜ
            foreach (var itemDto in orderDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);

                if (product == null || !product.IsActive)
                {
                    return new ErrorResult($"İşlem durduruldu: {itemDto.ProductId} referanslı ürün bulunamadı veya şu an satışa kapalı!");
                }

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    Note = itemDto.Note,
                    IsComplimentary = false
                };

                totalPrice += (orderItem.UnitPrice * orderItem.Quantity);
                order.OrderItems.Add(orderItem);
            }

            // 3. ADIM: TOPLAM FİYATI BELİRLEME
            order.TotalPrice = totalPrice;

            // 4. ADIM: VERİTABANINA KAYIT (COMMIT) VE MASAYI "DOLU" YAPMA ZEKASI
            await _orderRepository.AddAsync(order);

            // ==========================================
            // [YENİ EKLENDİ]: SİHİRLİ DOKUNUŞ (MASA DURUMUNU GÜNCELLEME)
            // ==========================================
            // Garson siparişi açtığı anda gidip o masayı veritabanından buluyoruz.
            // DİKKAT: "_tableRepository" kısmının altı kırmızı çizilecektir, bu harika! Birazdan onu da sisteme tanıtacağız.
            var table = await _tableRepository.GetByIdAsync(orderDto.TableId);
            if (table != null)
            {
                // Masayı boş olmaktan çıkarıp "Dolu" (Kırmızı yanacak) durumuna getiriyoruz.
                table.Status = TableStatus.Occupied;
                _tableRepository.Update(table);
            }
            // ==========================================

            await _unitOfWork.SaveChangesAsync();

            // 5. ADIM: SİSTEM GÜNLÜĞÜ (LOG) OLUŞTURMA
            // [DEĞİŞTİ]: Loglamada artık TableNumber yerine TableId kullanıyoruz ki tam izlenebilirlik sağlansın.
            _logger.LogInformation("YENİ SİPARİŞ! Adisyon: {OrderNo}, Masa ID: {TableId}, Tutar: {Total} TL, Garson: {WaiterId}",
                order.OrderNumber, order.TableId, order.TotalPrice, waiterId);

            return new SuccessResult($"Sipariş başarıyla mutfağa iletildi. Fiş No: {order.OrderNumber}");
        }
        // Business/Concretes/OrderManager.cs içine eklenecek metotlar:

        // 1. ADIM: MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        // Neden? Çünkü mutfak personelinin ödenmiş veya iptal edilmiş eski siparişlerle kafasını karıştırmıyoruz.
        // Business/Concretes/OrderManager.cs içindeki GetPendingOrdersAsync metodu:

        // 1. ADIM: MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        public async Task<IDataResult<List<Order>>> GetPendingOrdersAsync()
        {
            var orders = await _orderRepository.GetAll()

                // Mutfak personeli sadece "Bekleyen" (Pending) veya "Hazırlanıyor" (Preparing) olan adisyonları görmeli.
                .Where(x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Preparing)

                // ==========================================
                // [YENİ ZEKÂ - FİLTRELİ YÜKLEME]: EF Core 5.0+ Özelliği
                // ==========================================
                // Adisyonun içindeki sipariş satırlarını (OrderItems) getirirken HEPSİNİ getirme!
                // Sadece IsStockDecreased == false olanları (Yani aşçının henüz mühürlemediği, yeni ürünleri) getir.
                .Include(x => x.OrderItems.Where(oi => oi.IsStockDecreased == false))

                    // O yeni satırların içinden geçip, ürün detaylarını (isim, fiyat) al.
                    .ThenInclude(oi => oi.Product)

                .ToListAsync();

            // ==========================================
            // [EKSTRA GÜVENLİK DUVARI]
            // ==========================================
            // Filtreleme yaptıktan sonra, eğer bir adisyonun içinde "mutfağın yapacağı hiçbir ürün kalmamışsa" 
            // (OrderItems listesi boşsa), o içi boş adisyon başlığını mutfak ekranında boşu boşuna gösterme.
            // Sadece içinde en az 1 tane yeni iş (Any) olan masaları listeye dahil et.
            var filteredOrders = orders.Where(o => o.OrderItems.Any()).ToList();

            return new SuccessDataResult<List<Order>>(filteredOrders, "Mutfak ekranı sadece YENİ eklentilerle başarıyla hazırlandı.");
        }
        // Siparişi "Hazır" (Ready) durumuna getirir ve stoğu otomatik düşer.
        public async Task<IResult> SetOrderReadyAsync(int orderId)
        {
            // 1. ADIM: Siparişi ve satırlarını getir.
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null)
            {
                return new ErrorResult("Sipariş bulunamadı!");
            }

            // 2. ADIM: AKILLI DÖNGÜ
            // Masadaki her bir ürünü tek tek geziyoruz.
            foreach (var item in order.OrderItems)
            {
                // [KRİTİK KONTROL]: Eğer bu ürünün stoğu daha önce düşülmediyse (IsStockDecreased == false)
                // Bu sayede masaya sonradan eklenen ürünlerin stoğu düşerken, eskilerin stoğu sabit kalır.
                if (!item.IsStockDecreased)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);

                    if (product != null)
                    {
                        // Stok miktarını düşür.
                        product.StockQuantity -= item.Quantity;

                        // [MÜHÜRLEME]: Bu satırın stoğunu düştük diye işaretliyoruz.
                        item.IsStockDecreased = true;

                        _productRepository.Update(product);
                    }
                }
            }

            // 3. ADIM: DURUMU GÜNCELLE
            // Masada bekleyen yeni bir şey kalmadığı için durumu tekrar 'Ready' yapıyoruz.
            order.Status = OrderStatus.Ready;

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult($"Sipariş {order.OrderNumber} başarıyla hazırlandı. Sadece yeni eklenen ürünlerin stokları düşüldü.");
        }
        // ==========================================
        // SİPARİŞİ KAPATMA (ÖDEME ALMA VE MASAYI BOŞALTMA) ZEKASI
        // ==========================================
        public async Task<IResult> CloseOrderAsync(int orderId)
        {
            // 1. ADIM: ADİSYONU BULMA VE KONTROL
            // Kasiyerin girdiği orderId (Adisyon Numarası) ile veritabanına gidip siparişi buluyoruz.
            var order = await _orderRepository.GetByIdAsync(orderId);

            // Eğer güvenlik gereği böyle bir sipariş yoksa işlemi hemen durduruyoruz.
            if (order == null)
            {
                return new ErrorResult("Kapatılacak sipariş bulunamadı!");
            }

            // GÜVENLİK DUVARI: Sadece 'Hazır' (3) veya 'Teslim Edildi' (4) olan siparişler kapatılabilir.
            // Bekleyen, mutfakta hazırlanan veya iptal edilmiş bir siparişin ödemesi (yanlışlıkla bile olsa) alınamaz!
            if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Delivered)
            {
                return new ErrorResult("Bu sipariş henüz ödeme almak için uygun durumda değil (Mutfakta olabilir veya iptal edilmiş).");
            }

            // 2. ADIM: ADİSYONU "ÖDENDİ" YAPMA
            // Fişin durumunu enum listendeki 'Completed' (Yani 5 numaralı durum) olarak işaretliyoruz.
            order.Status = OrderStatus.Completed;
            _orderRepository.Update(order); // Veritabanına "Adisyonu güncelleyeceğiz, aklında tut" diyoruz.

            // 3. ADIM: MASA BOŞALTMA (SİHRİN OLDUĞU YER)
            // Kapatılan adisyonun hangi masaya ait olduğunu (TableId) biliyoruz. Gidip o masayı buluyoruz.
            var table = await _tableRepository.GetByIdAsync(order.TableId);

            // Masayı bulduysak, üzerindeki "Dolu" (Kırmızı) tabelasını indirip tekrar "Boş" (Yeşil) yapıyoruz.
            if (table != null)
            {
                table.Status = Core.Concretes.Enums.TableStatus.Empty;
                _tableRepository.Update(table); // Veritabanına "Masayı da güncelleyeceğiz" diyoruz.
            }

            // 4. ADIM: DEĞİŞİKLİKLERİ KALICI YAPMA (COMMIT)
            // Hem Adisyonun durumunu (Completed) hem de Masanın durumunu (Empty) TEK BİR PAKET halinde veritabanına kaydediyoruz.
            await _unitOfWork.SaveChangesAsync();

            // 5. ADIM: SİSTEM GÜNLÜĞÜ (LOG)
            // Muhasebe veya patron için "Şu masadan şu kadar para alındı" diye kalıcı not düşüyoruz.
            _logger.LogInformation("ÖDEME ALINDI! Sipariş No: {OrderNo}, Boşalan Masa ID: {TableId}, Kasa Girişi: {TotalPrice} TL",
                order.OrderNumber, order.TableId, order.TotalPrice);

            // Kasiyere / Garsona işlemin başarıyla bittiğini haber veriyoruz.
            return new SuccessResult($"Sipariş {order.OrderNumber} başarıyla kapatıldı. Masa boşaltıldı. Tahsilat: {order.TotalPrice} TL");
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

            return new SuccessResult($"{order.TableId}'in {order.OrderNumber} fişli siparişi başarıyla iptal edildi. Sebep: {cancellationReason}");
        }
        public async Task<IResult> MakeItemComplimentaryAsync(int orderId, int orderItemId)
        {
            // 1. ADIM: Doğrudan o adisyon satırını (OrderItem) bulalım.
            // _orderItemRepository üzerinden Include yaparak Product bilgisini de çekiyoruz.
            var item = await _orderItemRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == orderItemId && x.OrderId == orderId);

            if (item == null)
            {
                return new ErrorResult("İkram edilmek istenen satır bulunamadı.");
            }

            // 2. ADIM: Bağlı olduğu ana siparişi çekelim.
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("İşlem yapılamaz: Sipariş bulunamadı, kapanmış veya iptal edilmiş.");
            }

            if (item.IsComplimentary)
            {
                return new ErrorResult("Bu ürün zaten ikram edilmiş.");
            }

            // 3. ADIM: İŞLEMİ YAP
            item.IsComplimentary = true;

            // Toplam fiyattan (Birim Fiyat * Adet) kadar düşüyoruz.
            order.TotalPrice -= (item.UnitPrice * item.Quantity);

            // 4. ADIM: GÜNCELLE VE KAYDET
            _orderRepository.Update(order);
            _orderItemRepository.Update(item);

            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("İkram işlemi başarıyla tamamlandı.");
        }
        // AddItemsToOrderAsync: Açık olan bir masaya "Ek Sipariş" girilmesini sağlar.
        // Örn: Müşteri yemeğini yedi, üzerine bir de tatlı ve kahve istediğinde bu metot çalışır.
        public async Task<IResult> AddItemsToOrderAsync(AddItemsToOrderDto addItemsDto)
        {
            // 1. ADIM: HEDEF ADİSYONU TESPİT ETME
            // Veritabanından üzerine ekleme yapacağımız siparişi buluyoruz.
            // .Include(x => x.OrderItems) kullanıyoruz çünkü mevcut adisyonun içine yeni kalemler yerleştireceğiz; 
            // liste yüklü gelmezse (null olursa) ekleme yapamayız.
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == addItemsDto.OrderId);

            // [KRİTİK KONTROL]: Sipariş veritabanında hiç yoksa işlemi durdur.
            if (order == null)
            {
                return new ErrorResult("Üzerine ekleme yapılacak sipariş bulunamadı!");
            }

            // [GÜVENLİK DUVARI]: Sadece "Yaşayan" siparişlere ekleme yapılabilir.
            // Eğer hesap çoktan ödenmişse (Completed) veya sipariş iptal edilmişse (Canceled), 
            // o adisyon tarih olmuştur. Kapalı hesaba sonradan ürün eklenmesini engelleyerek yolsuzluğu önlüyoruz.
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Bu adisyon kapalı olduğu için yeni ürün eklenemez. Lütfen yeni bir masa açın.");
            }

            // Eklenen ürünlerin toplam maliyetini tutacağımız geçici kasa.
            decimal additionalPrice = 0;

            // 2. ADIM: YENİ ÜRÜNLERİ TEK TEK KONTROL EDİP ADİSYONA İŞLEME
            foreach (var itemDto in addItemsDto.Items)
            {
                // [FİYAT GÜVENLİĞİ]: Ürün fiyatını asla tabletten gelen veriye göre belirlemiyoruz.
                // Veritabanına (ana menüye) gidip ürünün güncel ve gerçek fiyatını çekiyoruz.
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);

                // Ürün silinmişse veya restoran yönetimi tarafından "Satışa Kapalı" (IsActive = false) yapılmışsa ekletmiyoruz.
                if (product == null || !product.IsActive)
                {
                    return new ErrorResult($"{itemDto.ProductId} referanslı ürün şu an menüde aktif değil.");
                }

                // Yeni adisyon satırını (OrderItem) hafızada hazırlıyoruz.
                var newOrderItem = new OrderItem
                {
                    OrderId = order.Id,   // Bu satırı hangi masaya bağlayacağımızı söylüyoruz.
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price, // O anki güncel liste fiyatını sabitleyerek kaydediyoruz.
                    Note = itemDto.Note,       // Müşterinin özel isteği (Örn: "Tatlı az şerbetli olsun")
                    IsComplimentary = false    // Eklenen ürünler varsayılan olarak ücretlidir.
                };

                // Yeni ürünlerin tutarını hesaplayıp ek tutara yansıtıyoruz.
                additionalPrice += (newOrderItem.UnitPrice * newOrderItem.Quantity);

                // Hazırladığımız bu yeni satırı, adisyonun mevcut ürün listesine ekliyoruz.
                order.OrderItems.Add(newOrderItem);
            }

            // 3. ADIM: ANA FATURAYI GÜNCELLEME
            // Mevcut toplamın üzerine sadece yeni gelenlerin fiyatını ekliyoruz. 
            // Böylece eski toplamı bozmadan güncel rakama ulaşıyoruz.
            order.TotalPrice += additionalPrice;

            // 4. ADIM: OPERASYONEL DURUM YÖNETİMİ
            // Eğer masa o ana kadar "Hazır" (Ready) durumundaysa, yeni eklenen ürünler henüz pişmediği için
            // siparişi tekrar "Hazırlanıyor" (Preparing) durumuna çekiyoruz. 
            // Böylece mutfak ekranında bu masa yeniden "Yeni İş Var!" şeklinde parlamaya başlar.
            if (order.Status == OrderStatus.Ready || order.Status == OrderStatus.Delivered)
            {
                order.Status = OrderStatus.Preparing;
            }

            // 5. ADIM: TÜM DEĞİŞİKLİKLERİ TEK SEFERDE KAYDETME (COMMIT)
            // Hem yeni ürün satırlarını hem de siparişin güncellenen toplam fiyatını tek bir işlemde veritabanına yazıyoruz.
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            // Patron için arkada bir iz bırakıyoruz.
            _logger.LogInformation("EK SİPARİŞ GİRİLDİ: {OrderNo} nolu masaya {Amount} TL değerinde ilave yapıldı.",
                order.OrderNumber, additionalPrice);

            return new SuccessResult("Ek siparişler başarıyla eklendi ve mutfağa iletildi.");
        }
        // ==========================================
        // KASA OPERASYONU: İNDİRİM UYGULAMA
        // ==========================================
        public async Task<IResult> ApplyDiscountAsync(int orderId, decimal discountAmount)
        {
            // 1. ADIM: Adisyonu bul
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                return new ErrorResult("İndirim uygulanacak sipariş bulunamadı!");
            }

            // [GÜVENLİK DUVARI 1]: Kapanmış hesaba veya iptal edilmiş siparişe indirim yapılamaz (Geçmişe dönük yolsuzluğu önler).
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Kapanmış veya iptal edilmiş bir hesaba indirim uygulanamaz!");
            }   

            // [GÜVENLİK DUVARI 2]: İndirim tutarı, adisyonun toplam tutarından büyük olamaz. 
            // (Müşteriye üste para vermemek için)
            if (discountAmount < 0 || discountAmount > order.TotalPrice)
            {
                return new ErrorResult($"Geçersiz indirim tutarı! İndirim en az 0, en fazla {order.TotalPrice} TL olabilir.");
            }

            // 2. ADIM: MATEMATİK VE KAYIT
            // İndirim miktarını arşive (veritabanına) not düşüyoruz ki gün sonunda patron ne kadar indirim yapıldığını görsün.
            order.DiscountAmount = discountAmount;

            // Faturanın son ödeme tutarından indirimi düşüyoruz.
            order.TotalPrice -= discountAmount;

            // 3. ADIM: VERİTABANINA YAZ
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            // Arka planda patron için log bırakıyoruz.
            _logger.LogInformation("İNDİRİM YAPILDI: {OrderNo} nolu adisyona {Discount} TL indirim uygulandı. Yeni Tutar: {Total} TL",
                order.OrderNumber, discountAmount, order.TotalPrice);

            return new SuccessResult($"İndirim başarıyla uygulandı. Ödenecek yeni tutar: {order.TotalPrice} TL");
        }
    }
}