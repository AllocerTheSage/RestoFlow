namespace Business.DTOs.OrderDtos
{
    public class OrderItemDto
    {
        public int ProductId { get; set; } // Hangi yemek/içecek?
        public int Quantity { get; set; }  // Kaç porsiyon?
        public string? Note { get; set; }  // Müşteri notu (Örn: "Acısız")

        // IsComplimentary (İkram) gibi yetki gerektiren durumları 
        // garson direkt siparişte seçemesin diye buraya koymuyoruz. 
        // Onu sonradan Patron/Şef özel bir uçtan (Endpoint) yapacak.
    }
}