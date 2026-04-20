using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities
{
    public class Order : BaseEntity
    {
        // Fişin tepesinde yazan o karmaşık harfli kod (Örn: A7B2C9)
        public string OrderNumber { get; set; } = string.Empty;

        // ==========================================
        // [SİLİNDİ] -> public string TableNumber { get; set; }
        // Neden Sildik? 
        // Çünkü masayı sadece "Masa-5" diye bir metin (string) olarak tutmak çok tehlikeliydi.
        // Garson yanlışlıkla "Msa-5" yazsa sistem bunu anlamazdı ve masanın dolu mu boş mu olduğunu bilemezdik.
        // ==========================================

        // ==========================================
        // [YENİ EKLENDİ] -> AKILLI MASA BAĞLANTISI (RELATION)
        // ==========================================

        // 1. Veritabanının Dili (Foreign Key - Yabancı Anahtar)
        // Siparişin hangi masaya ait olduğunu tutan değişmez matematiksel kimlik (Örn: Masa ID = 14).
        // İsimler ("VIP-1", "Teras-5") değişse bile bu ID asla şaşmaz.
        public int TableId { get; set; }

        // 2. C#'ın Dili (Navigation Property - Gezinme Özelliği)
        // Entity Framework'ün sihirli değneği. Bu sayede kod yazarken gidip veritabanında ekstra arama yapmadan
        // doğrudan "order.Table.Capacity" yazarak o masanın kaç kişilik olduğunu öğrenebileceğiz.
        public virtual Table Table { get; set; }
        // ==========================================

        // Siparişi alan garsonun kimliği (Token'dan geliyor)
        public string WaiterId { get; set; } = string.Empty;

        // Adisyonun toplam tutarı
        public decimal TotalPrice { get; set; } = 0;

        // Adisyonun mutfaktaki ve kasadaki güncel durumu
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Opsiyonel Müşteri Bilgileri
        public string? CustomerName { get; set; }
        public int? GuestCount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? CancellationReason { get; set; }

        // Bu adisyonun içindeki sipariş satırları (Hamburgerler, Kolalar vb.)
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}