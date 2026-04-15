using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    // BaseEntity'den miras alıyoruz çünkü Id, CreatedDate gibi ortak alanlara ihtiyacımız var.
    public class Product : BaseEntity
    {
        // Ürünün adını tutar (Örn: Hamburger, Kola)
        public string Name { get; set; } = string.Empty;

        // Ürünün satış fiyatını tutar
        public decimal Price { get; set; }

        // Mutfak rolünün müdahale edeceği stok miktarını tutar
        public int Stock { get; set; }
    }
}