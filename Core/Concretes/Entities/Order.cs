using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty;

        // Hangi Masa?
        public string TableNumber { get; set; } = string.Empty;

        // Siparişi Hangi Garson Aldı? (Kasadaki hesabı kimin açtığı)
        public string WaiterId { get; set; } = string.Empty;

        // Toplam Tutar
        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // ==========================================
        // YENİ EKLENEN NULLABLE (?) DETAYLAR
        // ==========================================

        // Müşteri Adı (Opsiyonel: VIP müşteriler veya paket servis ihtimali için)
        public string? CustomerName { get; set; }

        // Kişi Sayısı (Opsiyonel: "Kişi başı ortalama harcama" raporu için patron bayılır buna)
        public int? GuestCount { get; set; }

        // İndirim Tutarı (Opsiyonel: Eğer [ApplyDiscount] yetkisi kullanıldıysa buraya yazılır)
        public decimal? DiscountAmount { get; set; }

        // Adisyon İptal Sebebi (Opsiyonel: Eğer tüm masa iptal edildiyse patron nedenini görmek ister)
        // Örn: "Müşteri çok beklediği için kızıp kalktı"
        public string? CancellationReason { get; set; }

        // ==========================================

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}