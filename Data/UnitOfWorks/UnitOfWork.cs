using Core.Abstracts;
using Data.Contexts;

namespace Data.UnitOfWorks
{
    // Core'daki IUnitOfWork arayüzünü uygulayan somut sınıf.
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        // Constructor: Veritabanı context'ini enjekte ediyoruz.
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Bellekte bekleyen tüm değişiklikleri tek seferde SQLite veritabanına yazar.
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // İşlem bittiğinde veritabanı bağlantısını güvenli bir şekilde kapatır.
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}