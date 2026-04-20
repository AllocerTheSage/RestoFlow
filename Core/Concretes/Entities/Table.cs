using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities
{
    public class Table : BaseEntity
    {
        // Masanın ekranda görünecek adı (Örn: "Masa-1", "Bahçe-5", "VIP-1")
        public string TableNumber { get; set; } = string.Empty;

        // Masanın kişi kapasitesi (Örn: 2 kişilik, 4 kişilik masa)
        public int Capacity { get; set; }

        // Masanın o anki durumu (Boş, Dolu, Rezerve). 
        // Varsayılan olarak her yeni masa "Boş" (Empty) başlar.
        public TableStatus Status { get; set; } = TableStatus.Empty;

        // ==========================================
        // İLİŞKİLER (Navigation Properties)
        // ==========================================

        // Bir masanın geçmişten bugüne birden fazla siparişi (Adisyonu) olabilir.
        // Bu liste sayesinde "Masa-5'in geçmiş tüm siparişlerini getir" diyebileceğiz.
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}