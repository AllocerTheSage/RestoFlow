using Core.Concretes.Enums;

namespace Business.DTOs.TableDtos
{
    // Saha haritası (Dashboard) için dış dünyaya gönderilecek paket
    public class TableDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public TableStatus Status { get; set; }

        // [İŞTE SİHİR BURADA] 
        // Masa doluysa (Kırmızıysa), üzerinde ne kadarlık bir açık hesap olduğunu gösterecek alan.
        public decimal ActiveOrderTotal { get; set; }
    }
}