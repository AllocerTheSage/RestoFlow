namespace Business.DTOs.TableDtos
{
    public class TableUpdateDto
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // YENİ: Kategori değişirse diye yeni ID'yi alıyoruz!
        public int CategoryId { get; set; }
    }
}