using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty; // Hata veren kısım burasıydı, ekledik.
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }

        // İlişkiler
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}