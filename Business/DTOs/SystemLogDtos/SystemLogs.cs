namespace Business.DTOs.LogDtos
{
    public class SystemLogDto
    {
        public int Id { get; set; }

        // Enum numarası yerine Frontend'e okunabilir isim gitsin diye String tutuyoruz.
        public string LogType { get; set; } = string.Empty;

        // AppUser koca bir sınıf, biz sadece adamın adını-soyadını istiyoruz.
        public string UserName { get; set; } = "Sistem";

        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int? RelatedEntityId { get; set; }

        // Olayın gerçekleştiği tam saat
        public DateTime CreatedDate { get; set; }
    }
}