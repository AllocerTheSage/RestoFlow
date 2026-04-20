namespace Business.DTOs.OrderDtos
{
    public class AddItemsToOrderDto
    {
        // Üzerine ekleme yapılacak mevcut siparişin ID'si
        public int OrderId { get; set; }

        // Masaya sonradan eklenen yeni ürünlerin listesi
        // Daha önce oluşturduğumuz OrderItemDto'yu burada tekrar kullanarak kod tekrarını önlüyoruz.
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}