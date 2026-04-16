namespace Business.DTOs.OrderDtos
{
    public class OrderCreateDto
    {
        public string TableNumber { get; set; } = string.Empty; // Hangi Masa?

        // Garsonun ID'sini DTO'dan almıyoruz! Çünkü kötü niyetli biri başkasının ID'sini yazabilir.
        // Biz Garsonun ID'sini direkt Token'ın (Yaka Kartının) içinden çekeceğiz. (Güvenlik!)

        // Masadaki kişi sayısı (Opsiyonel girilebilir)
        public string? CustomerName { get; set; }
        public int? GuestCount { get; set; }

        // Sipariş edilen ürünlerin listesi
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}