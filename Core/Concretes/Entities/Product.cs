using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int StockQuantity { get; set; } 

        // MUTFAK İÇİN: Ürünü manuel olarak satışa kapatıp açmaya yarar.
        public bool IsActive { get; set; } = true;
        public bool IsReturnable { get; set; } = false; //
    }
}