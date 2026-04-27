using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities
{
    public class Table : BaseEntity
    {
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Empty;

        // ==========================================
        // YENİ: KATEGORİ İLİŞKİSİ (FOREIGN KEY)
        // ==========================================
        // Artık "string Category" yok. Onun yerine "CategoryId" var!
        // Sistem masanın hangi kategoriye ait olduğunu bu ID üzerinden bilecek.
        public int CategoryId { get; set; }
        public virtual TableCategory Category { get; set; } = null!;

        // ==========================================
        // İLİŞKİLER (Navigation Properties)
        // ==========================================
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}