using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    // Adisyonun içindeki her bir satırı temsil eden Class
    public class OrderItem : BaseEntity
    {
        // Kaç adet sipariş verildi?
        public int Quantity { get; set; }

        // Ürünün o anki fiyatı (Ürün fiyatı değişse bile adisyon sabit kalmalı)
        public decimal UnitPrice { get; set; }

        // Hangi adisyona ait? (Dış Anahtar - Foreign Key)
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        // Hangi ürün sipariş edildi? (Dış Anahtar - Foreign Key)
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}