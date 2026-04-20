using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    public class OrderItem : BaseEntity
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Müşteri Notu (Mutfak ekranında kırmızı yazıyla çıkacak)
        // Örn: "Acısız", "Buzsuz", "Çok pişmiş"
        public string? Note { get; set; }

        // ==========================================
        // YENİ EKLENEN DETAYLAR
        // ==========================================

        // Bu ürün İkram mı? (Default false. Eğer true ise fiyat 0 hesaplanır)
        // Patronun yetkilerinde yazdığımız [AddComplimentary] burayı tetikler.
        public bool IsComplimentary { get; set; } = false;

        // İade/İptal Sebebi (Opsiyonel: Ürün mutfakta yandı mı, müşteri mi beğenmedi?)
        // Patronun [ProcessReturn] yetkisi kullanıldığında burası doldurulur.
        public string? ReturnedReason { get; set; }

        // [AKILLI STOK TAKİBİ]
        // Bu alan, ürünün stok miktarının veritabanından düşülüp düşülmediğini kontrol eder.
        // Neden ekledik? Masaya sonradan ürün eklendiğinde ve mutfak tekrar "Hazır" dediğinde,
        // eski ürünlerin stoklarının tekrar tekrar (yanlışlıkla) düşülmesini engeller.
        public bool IsStockDecreased { get; set; } = false;

        // ==========================================

        // İlişkiler
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}