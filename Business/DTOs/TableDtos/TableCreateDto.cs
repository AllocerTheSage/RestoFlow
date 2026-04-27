namespace Business.DTOs.TableDtos
{
    public class TableCreateDto
    {
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // YENİ: Artık metin (string) değil, Kategorinin ID'sini alıyoruz!
        public int CategoryId { get; set; }
    }
}