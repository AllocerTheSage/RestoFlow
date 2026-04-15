namespace Core.Abstracts
{
    // Veritabanı üzerindeki tüm işlemleri (Ekle, Sil, Güncelle) tek bir kanal üzerinden yönetir.
    // IDisposable: İşlem bittiğinde veritabanı bağlantısının bellekten atılmasını sağlar.
    public interface IUnitOfWork : IDisposable
    {
        // Yapılan tüm değişiklikleri tek bir paket halinde veritabanına kaydeder.
        // Geriye etkilenen satır sayısını döner.
        Task<int> SaveChangesAsync();
    }
}