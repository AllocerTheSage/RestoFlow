using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    // Kasanın Ödeme Günlüğü
    public class Payment : BaseEntity
    {
        // Ödenen miktar (Örn: 250 TL)
        public decimal Amount { get; set; }

        // Ödeme Tipi (Nakit, Kredi Kartı, Yemek Kartı vb.)
        public string PaymentMethod { get; set; } = string.Empty;

        // ==========================================
        // İLİŞKİLER
        // ==========================================
        // Bu ödemenin hangi adisyona (masaya) ait olduğu
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
    }
}