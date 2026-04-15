using System.Linq.Expressions;
using Core.Abstracts.Bases;
using Core.Abstracts.IRepositories;
using Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    // Core'daki arayüzü (IGenericRepository) uygulayan somut sınıf.
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity, new()
    {
        protected readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        // Constructor: Veritabanı context'ini alıp ilgili tabloyu (dbSet) seçer.
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // Tüm verileri asenkron sorgulanabilir şekilde getirir.
        public IQueryable<T> GetAll()
        {
            // AsNoTracking: Veriyi sadece okumak için çeker, takip etmez (Performans artırır).
            return _dbSet.AsNoTracking().AsQueryable();
        }

        // Id'ye göre tek bir kayıt bulur.
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        // Listeye yeni bir veri ekler.
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        // Mevcut veriyi güncellenmiş olarak işaretler.
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        // Veriyi silinmiş olarak işaretler.
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        // Belirli bir kritere göre (Expression) filtreleme yapar.
        public IQueryable<T> Where(Expression<Func<T, bool>> expression)
        {
            return _dbSet.Where(expression);
        }
    }
}