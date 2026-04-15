using System;

namespace Core.Abstracts.Bases
{
    public abstract class BaseEntity
    {
        // Tüm tablolarda (Sipariş, Ürün vb.) ortak olarak bulunacak kolonlar:
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now; // Veri eklendiği anki tarihi otomatik alır
        public DateTime? UpdatedDate { get; set; } // Güncelleme tarihi (Başlangıçta boş olabilir, o yüzden '?')
        public string? CreatedBy { get; set; } // Veriyi kimin eklediğini tutar
        public string? UpdatedBy { get; set; } // Veriyi kimin güncellediğini tutar
        public bool IsActive { get; set; } = true; // Ürün menüden kaldırılırsa silmek yerine bunu false yapacağız (Soft Delete mantığı)
    }
}