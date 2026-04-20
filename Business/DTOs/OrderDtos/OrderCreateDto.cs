namespace Business.DTOs.OrderDtos
{
    public class OrderCreateDto
    {
        // ==========================================
        // [SİLİNDİ] -> public string TableNumber { get; set; } = string.Empty;
        // NEDEN SİLDİK? 
        // Çünkü masaları artık garsonun klavyeden girdiği "Masa-5" gibi metinlerle (string) takip etmeyeceğiz.
        // Metinler hataya çok açıktır (yanlışlıkla "Msa-5" yazılabilir).
        // ==========================================

        // ==========================================
        // [YENİ EKLENDİ] -> MASA KİMLİĞİ (FOREIGN KEY BEKLENTİSİ)
        // NEDEN EKLEDİK?
        // Garson ön yüz (Frontend) ekranında yeşil renkli "Teras-5" masasına tıklayacak, 
        // ancak arka planda bu DTO aracılığıyla bize o masanın veritabanındaki değişmez 
        // matematiksel kimliğini (Örn: TableId = 14) gönderecek. Sistem asla şaşmayacak.
        // ==========================================
        public int TableId { get; set; }

        // Garsonun ID'sini DTO'dan almıyoruz! Çünkü kötü niyetli biri başkasının ID'sini yazabilir.
        // Biz Garsonun ID'sini direkt Token'ın (Yaka Kartının) içinden çekeceğiz. (Güvenlik!)

        // Masadaki kişi sayısı (Opsiyonel girilebilir)
        public string? CustomerName { get; set; }
        public int? GuestCount { get; set; }

        // Sipariş edilen ürünlerin listesi
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}