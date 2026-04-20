using Business.Abstracts;
using Business.DTOs.PaymentDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Concretes
{
    public class PaymentManager : IPaymentService
    {
        // ==========================================
        // ASİSTANLARIMIZ (Bağımlılıklar)
        // ==========================================
        private readonly IGenericRepository<Payment> _paymentRepository; // Ödeme makbuzlarını kaydeden defter
        private readonly IGenericRepository<Order> _orderRepository;     // Adisyonların (hesapların) tutulduğu dosya
        private readonly IGenericRepository<Table> _tableRepository;     // Masaların durumunu (Dolu/Boş) tutan liste
        private readonly IUnitOfWork _unitOfWork;                       // Tüm işlemleri tek seferde onaylayan mühür
        private readonly ILogger<PaymentManager> _logger;               // Arka planda olay günlüğü tutan sistem

        public PaymentManager(
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<Order> orderRepository,
            IGenericRepository<Table> tableRepository,
            IUnitOfWork unitOfWork,
            ILogger<PaymentManager> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ====================================================================
        // ANA OPERASYON: ÖDEME ALMA VE HESAP KAPATMA
        // ====================================================================
        public async Task<IResult> ReceivePaymentAsync(CreatePaymentDto paymentDto)
        {
            // 1. ADIM: İLGİLİ ADİSYONU BULMAK
            // Masanın durumuna müdahale edebilmek için "Include(o => o.Table)" ile masayı da peşine takıyoruz.
            var order = await _orderRepository.GetAll()
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == paymentDto.OrderId);

            // GÜVENLİK KONTROLÜ: Eğer veritabanında böyle bir adisyon yoksa işlemi durdur.
            if (order == null) return new ErrorResult("Hata: Ödeme yapılmak istenen adisyon bulunamadı.");

            // GÜVENLİK KONTROLÜ: Zaten ödenmiş (Completed) veya iptal edilmiş (Canceled) masaya tekrar ödeme alınamaz.
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Canceled)
            {
                return new ErrorResult("Güvenlik İhlali: Kapalı veya iptal edilmiş bir hesaba ödeme girişi yapılamaz.");
            }

            // 2. ADIM: ÖDEME KAYDINI OLUŞTURMAK (Makbuz Kesmek)
            // Kasiyerin gönderdiği "200 TL - Nakit" gibi bilgileri Payments tablosuna yeni bir satır olarak ekliyoruz.
            var payment = new Payment
            {
                OrderId = paymentDto.OrderId,
                Amount = paymentDto.Amount,
                PaymentMethod = paymentDto.PaymentMethod
            };
            await _paymentRepository.AddAsync(payment);

            // 3. ADIM: ADİSYON ÜZERİNDEKİ "ÖDENEN MİKTAR"I GÜNCELLEMEK
            // Masanın toplam borcundan ne kadarının ödendiğini takip ettiğimiz PaidAmount sütununa ekleme yapıyoruz.
            // Örn: Eski ödenen 100 TL + Yeni gelen 200 TL = Toplam 300 TL ödendi.
            order.PaidAmount += paymentDto.Amount;
            _orderRepository.Update(order);

            // 4. ADIM: HESAP BİTTİ Mİ? (ALMAN USULÜ FİNALİ)
            // Eğer ödenen toplam miktar, adisyonun genel toplamına (TotalPrice) ulaştıysa veya geçtiyse...
            if (order.PaidAmount >= order.TotalPrice)
            {
                // A) Adisyonun durumunu "Tamamlandı / Ödendi" yapıyoruz.
                order.Status = OrderStatus.Completed;

                // B) Masayı "Boş (Empty / Yeşil)" hale getiriyoruz ki yeni müşteri oturabilsin.
                if (order.Table != null)
                {
                    order.Table.Status = TableStatus.Empty;
                    _tableRepository.Update(order.Table);
                }

                _logger.LogInformation("FİNAL: {OrderNo} nolu adisyonun tüm ödemeleri tamamlandı. Masa boşaltıldı.", order.OrderNumber);
            }
            else
            {
                // Eğer hala borç varsa, kasiyere ne kadar kaldığını haber veriyoruz.
                decimal kalan = order.TotalPrice - order.PaidAmount;
                _logger.LogInformation("KISMİ ÖDEME: {OrderNo} nolu adisyona {Amount} TL ödendi. Kalan borç: {Kalan} TL",
                    order.OrderNumber, paymentDto.Amount, kalan);
            }

            // 5. ADIM: TÜM DEĞİŞİKLİKLERİ TEK BİR ATOMİK İŞLEMDE KAYDETMEK
            // Eğer yukarıdaki işlemlerden herhangi biri (masayı boşaltma, ödeme kaydetme vb.) hata verirse, 
            // SaveChangesAsync sayesinde hiçbir veriyi bozmadan işlemi geri alabiliriz.
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Ödeme kaydı başarıyla alındı. Sistem güncellendi.");
        }
    }
}