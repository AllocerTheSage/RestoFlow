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
            _logger.LogInformation("YENİ SİPARİŞ! Adisyon: {OrderNumber}, Masa ID: {TableId}, Tutar: {Total} TL, Garson: {WaiterId}",
                order.OrderNumber, order.TableId, order.TotalPrice, waiterId);

            return new SuccessResult($"Sipariş başarıyla mutfağa iletildi. Fiş No: {order.OrderNumber}");
        }
        // Business/Concretes/OrderManager.cs içine eklenecek metotlar:

        // 1. ADIM: MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        // Neden? Çünkü mutfak personelinin ödenmiş veya iptal edilmiş eski siparişlerle kafasını karıştırmıyoruz.
        // Business/Concretes/OrderManager.cs içindeki GetPendingOrdersAsync metodu:

        // 1. ADIM: MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        // YENİ METOT: SİPARİŞİ HAZIRLAMAYA BAŞLA
        // 1. MUTFAK EKRANI İÇİN BEKLEYEN SİPARİŞLERİ LİSTELEME
        public async Task<IDataResult<List<Order>>> GetPendingOrdersAsync()
        {
            var orders = await _orderRepository.GetAll()
                // DİKKAT: Ready (3) olanları da ekledik ki 3. sütunda (Hazır) görünsünler!
                .Where(x => x.Status == OrderStatus.Pending || x.Status == OrderStatus.Preparing || x.Status == OrderStatus.Ready)
                .Include(x => x.OrderItems.Where(oi => oi.IsStockDecreased == false))
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var filteredOrders = orders.Where(o => o.OrderItems.Any()).ToList();
            return new SuccessDataResult<List<Order>>(filteredOrders, "Mutfak verileri getirildi.");
        }

        // YENİ EKLENEN METOT: SİPARİŞİ HAZIRLAMAYA BAŞLA
        public async Task<IResult> StartPreparationAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return new ErrorResult("Sipariş bulunamadı!");

            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Preparing;
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();
                return new SuccessResult("Sipariş hazırlık aşamasına alındı.");
            }
            return new ErrorResult("Bu sipariş hazırlamaya başlamak için uygun değil.");
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
            _logger.LogInformation("ÖDEME ALINDI! Sipariş No: {OrderNumber}, Boşalan Masa ID: {TableId}, Kasa Girişi: {TotalPrice} TL",
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
        // AddItemsToOrderAsync: Açık olan bir masaya "Ek Sipariş" girilmesini sağlar.
        public async Task<IResult> AddItemsToOrderAsync(AddItemsToOrderDto addItemsDto)
        {
            // 1. ADIM: HEDEF ADİSYONU TESPİT ETME
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == addItemsDto.OrderId);

            // [KRİTİK KONTROL]: Sipariş veritabanında hiç yoksa işlemi durdur.
            if (order == null)
            {
                return new ErrorResult("Üzerine ekleme yapılacak sipariş bulunamadı!");
            }

            // [GÜVENLİK DUVARI]: Sadece "Yaşayan" siparişlere ekleme yapılabilir.
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Bu adisyon kapalı olduğu için yeni ürün eklenemez. Lütfen yeni bir masa açın.");
            }

            decimal additionalPrice = 0;

            // 2. ADIM: YENİ ÜRÜNLERİ TEK TEK KONTROL EDİP ADİSYONA İŞLEME
            foreach (var itemDto in addItemsDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);

                if (product == null || !product.IsActive)
                {
                    return new ErrorResult($"{itemDto.ProductId} referanslı ürün şu an menüde aktif değil.");
                }

                var newOrderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    Note = itemDto.Note,
                    IsComplimentary = false
                };

                additionalPrice += (newOrderItem.UnitPrice * newOrderItem.Quantity);
                order.OrderItems.Add(newOrderItem);
            }

            // 3. ADIM: ANA FATURAYI GÜNCELLEME
            order.TotalPrice += additionalPrice;

            // ==========================================
            // 4. ADIM: SİHİRLİ DOKUNUŞ (DURUM SIFIRLAMA)
            // ==========================================
            // Eğer sipariş o an "Hazırlanıyor" veya "Hazır" durumundaysa bile,
            // yeni bir ürün eklendiği için onu tekrar "ONAY BEKLEYENLER" (Pending) sütununa gönderiyoruz.
            if (order.Status != OrderStatus.Pending)
            {
                order.Status = OrderStatus.Pending;
            }

            // 5. ADIM: TÜM DEĞİŞİKLİKLERİ TEK SEFERDE KAYDETME (COMMIT)
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync();

            // Patron için arkada bir iz bırakıyoruz.
            _logger.LogInformation("EK SİPARİŞ GİRİLDİ: {OrderNumber} nolu masaya {Amount} TL değerinde ilave yapıldı ve Mutfak Onayına sunuldu.",
                order.OrderNumber, additionalPrice);

            return new SuccessResult("Ek siparişler başarıyla eklendi ve mutfak onayına gönderildi.");
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
            _logger.LogInformation("İNDİRİM YAPILDI: {OrderNumber} nolu adisyona {Discount} TL indirim uygulandı. Yeni Tutar: {Total} TL",
                order.OrderNumber, discountAmount, order.TotalPrice);

            return new SuccessResult($"İndirim başarıyla uygulandı. Ödenecek yeni tutar: {order.TotalPrice} TL");
        }
        // ==========================================
        // OPERASYON: MASA TAŞIMA (TABLE TRANSFER)
        // ==========================================
        public async Task<IResult> TransferTableAsync(TransferTableDto transferDto)
        {
            // 1. ADIM: MEVCUT ADİSYONU BUL VE KONTROL ET
            var order = await _orderRepository.GetByIdAsync(transferDto.OrderId);

            if (order == null)
            {
                return new ErrorResult("Taşınmak istenen sipariş/adisyon bulunamadı.");
            }

            // [GÜVENLİK DUVARI 1]: Sadece aktif (açık) olan masalar taşınabilir.
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Kapanmış veya iptal edilmiş bir adisyon başka masaya taşınamaz.");
            }

            // 2. ADIM: HEDEF MASAYI (YENİ MASAYI) KONTROL ET
            // Müşterinin geçmek istediği yeni masayı veritabanından çekiyoruz.
            var newTable = await _tableRepository.GetByIdAsync(transferDto.NewTableId);

            if (newTable == null)
            {
                return new ErrorResult("Hedef masa bulunamadı.");
            }

            // [GÜVENLİK DUVARI 2]: Hedef masa BOŞ olmak zorunda! Dolu masaya veya rezerve masaya müşteri oturtamayız.
            // (Üst üste müşteri oturmasını engeller)
            if (newTable.Status == TableStatus.Occupied)
            {
                return new ErrorResult("Hedef masa şu an dolu! Lütfen boş bir masa seçin.");
            }
            if (newTable.Status == TableStatus.Reserved)
            {
                return new ErrorResult("Hedef masa şu an rezerve! Lütfen boş bir masa seçin.");
            }

            // 3. ADIM: ESKİ MASAYI BUL VE BOŞALT (YEŞİL YAP)
            // Adisyonun şu anki masasını buluyoruz ve üzerindeki "Dolu" tabelasını kaldırıyoruz.
            var oldTable = await _tableRepository.GetByIdAsync(order.TableId);
            if (oldTable != null)
            {
                oldTable.Status = TableStatus.Empty;
                _tableRepository.Update(oldTable);
            }

            // 4. ADIM: ADİSYONU YENİ MASAYA BAĞLA VE YENİ MASAYI DOLDUR (KIRMIZI YAP)
            // Fişin üzerindeki eski masa numarasını silip, yeni masanın ID'sini yazıyoruz.
            order.TableId = transferDto.NewTableId;
            _orderRepository.Update(order);

            // Yeni masanın üzerine "Dolu" tabelasını asıyoruz.
            newTable.Status = TableStatus.Occupied;
            _tableRepository.Update(newTable);

            // 5. ADIM: TÜM DEĞİŞİKLİKLERİ TEK SEFERDE VERİTABANINA KAYDET (COMMIT)
            // 3 farklı tablodaki değişiklik tek bir hamleyle kaydedilir.
            await _unitOfWork.SaveChangesAsync();

            // Arka planda log defterine not düşüyoruz (Kim, nereden nereye geçti?)
            _logger.LogInformation("MASA TAŞINDI: {OrderNumber} numaralı adisyon, Masa ID {OldTableId} konumundan Masa ID {NewTableId} konumuna taşındı.",
                order.OrderNumber, oldTable?.Id, newTable.Id);

            return new SuccessResult($"{oldTable.TableNumber} Adisyonu {newTable.TableNumber} tarafına başarı ile taşındı!.");
        }
        // ==========================================
        // ADİSYONDAN ÜRÜN SİLME (CERRAHİ OPERASYON)
        // ==========================================
        public async Task<IResult> RemoveItemFromOrderAsync(int orderId, int orderItemId)
        {
            // 1. ADIM: SİLİNECEK ÜRÜNÜ BUL (Ve bağlı olduğu Product bilgisini de peşine tak)
            // Sadece OrderItem tablosundan silmek yetmez, ürünün stoğuna ve IsReturnable durumuna
            // bakacağımız için ".Include(oi => oi.Product)" yapıyoruz.
            var orderItem = await _orderItemRepository.GetAll()
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(x => x.Id == orderItemId && x.OrderId == orderId);

            // Eğer öyle bir ürün o masada yoksa işlemi durdur.
            if (orderItem == null)
            {
                return new ErrorResult("Silinmek istenen ürün bu adisyonda bulunamadı.");
            }

            // 2. ADIM: BAĞLI OLDUĞU ADİSYONU BUL VE GÜVENLİK KONTROLÜ YAP
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Kapanmış veya iptal edilmiş adisyonlardan ürün silinemez!");
            }

            // 3. ADIM: AKILLI STOK İADESİ (Senin Tasarımın)
            // Eğer mutfak bu ürünü hazırlamışsa ve stok çoktan düşmüşse...
            if (orderItem.IsStockDecreased)
            {
                // ...ve eğer bu ürün dolaba geri konabilen bir ürünse (Örn: Kutu Kola)
                if (orderItem.Product.IsReturnable)
                {
                    // Stoğu dolaba geri koyuyoruz.
                    orderItem.Product.StockQuantity += orderItem.Quantity;
                    _productRepository.Update(orderItem.Product);
                }
                // (Eğer IsReturnable 'false' ise, yani pişen bir hamburgerse bu 'if' bloğuna girmez. 
                // Stok düştüğüyle kalır ve ürün zararına yazılır.)
            }

            // 4. ADIM: ADİSYON TOPLAM FİYATINI DÜŞÜRME
            // Eğer bu ürün daha önceden "İkram" olarak işaretlenmediyse (ikramsa parası zaten sıfırlanmıştır),
            // ürünün tutarını faturadan düşüyoruz.
            if (!orderItem.IsComplimentary)
            {
                order.TotalPrice -= (orderItem.UnitPrice * orderItem.Quantity);
            }

            // 5. ADIM: SATIRI SİL VE KAYDET (COMMIT)
            // Ürünü masadan fiziken siliyoruz ve güncellenen fatura tutarını kaydediyoruz.
            _orderItemRepository.Delete(orderItem);
            _orderRepository.Update(order);

            await _unitOfWork.SaveChangesAsync();

            // Patron için arka planda iz bırakıyoruz.
            _logger.LogInformation("ÜRÜN SİLİNDİ: {OrderNumber} nolu adisyondan {Quantity} adet {ProductName} çıkarıldı.",
                order.OrderNumber, orderItem.Quantity, orderItem.Product.Name);

            return new SuccessResult("Ürün adisyondan başarıyla silindi. Fiyat (ve gerekliyse stok) güncellendi.");
        }
        // Masanın o an açık olan (kapanmamış veya iptal edilmemiş) adisyonunu getirir.
        // ==========================================
        // 11. MEVCUT (AKTİF) ADİSYONU GETİRME UCU
        // ==========================================
        public async Task<IDataResult<Order>> GetActiveOrderByTableIdAsync(int tableId)
        {
            // Veritabanına gidip "Bu masaya ait, henüz KAPANMAMIŞ veya İPTAL EDİLMEMİŞ bir sipariş var mı?" diye soruyoruz.
            // .Include ve .ThenInclude ile adisyonun içindeki ürünleri ve o ürünlerin detaylarını da (isim, fiyat vb.) pakete dahil ediyoruz.
            var order = await _orderRepository.GetAll()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(x => x.TableId == tableId && 
                                         (x.Status != OrderStatus.Completed && x.Status != OrderStatus.Canceled));

            // Eğer masa boşsa (aktif adisyon yoksa) frontend'e hata dönüyoruz. (Frontend bunu yakalayıp "Sepet Boş" ekranı çizecek)
            if (order == null)
            {
                return new ErrorDataResult<Order>("Bu masanın aktif bir adisyonu yok.");
            }
            
            // Eğer aktif bir adisyon bulduysak, içindeki ürünlerle birlikte frontend'e yolluyoruz.
            return new SuccessDataResult<Order>(order, "Aktif adisyon başarıyla getirildi.");
        }
    }
}