using Core.Concretes.Enums;

namespace Business.DTOs.TableDtos
{
    public class TableDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public TableStatus Status { get; set; }
        public decimal ActiveOrderTotal { get; set; }

        // YENİ: Ekranda "Bahçe" yazması için
        public string CategoryName { get; set; } = string.Empty;
    }
}