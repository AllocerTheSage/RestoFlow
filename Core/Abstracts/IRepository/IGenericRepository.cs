using System.Linq.Expressions;
using Core.Abstracts.Bases;

namespace Core.Abstracts.IRepositories
{
    // T: Üzerinde işlem yapacağımız tabloyu temsil eder. 
    // Bu tablonun mutlaka BaseEntity'den miras alması gerektiğini 'where' ile şart koşuyoruz.
    public interface IGenericRepository<T> where T : BaseEntity, new()
    {
        // Veritabanındaki tüm kayıtları listelemek için kullanılır.
        IQueryable<T> GetAll();

        // Id numarasına göre tek bir kayıt getirmek için kullanılır.
        Task<T?> GetByIdAsync(int id);

        // Yeni bir veri eklemek için kullanılır.
        Task AddAsync(T entity);

        // Mevcut bir veriyi güncellemek için kullanılır.
        void Update(T entity);

        // Bir veriyi silmek için kullanılır.
        void Delete(T entity);

        // Özel filtreleme yapmak için kullanılır (Örn: Fiyatı 50'den büyük olanları getir).
        IQueryable<T> Where(Expression<Func<T, bool>> expression);
    }
}