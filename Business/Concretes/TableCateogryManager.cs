using Business.Abstracts;
using Business.DTOs.TableCategoryDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;     // TableCategory varlığımızı tanımak için
using Core.Concretes.Results;      // Başarılı/Hatalı sonuç dönmek için
using Microsoft.EntityFrameworkCore; // Veritabanı sorgu yardımcıları (ToListAsync vb.) için

namespace Business.Concretes
{
    // TableCategoryManager, kategorilerle ilgili tüm "iş mantığını" (Business Logic) yöneten sınıftır.
    public class TableCategoryManager : ITableCategoryService
    {
        // _categoryRepository: Veritabanında TableCategory tablosuna ekleme, silme, listeleme yapan ana aracımızdır.
        private readonly IGenericRepository<TableCategory> _categoryRepository;

        // _unitOfWork: Veritabanında yaptığımız tüm işlemleri (Ekle, Sil, Güncelle) 
        // tek bir seferde onaylayıp "kaydet" tuşuna basan mekanizmadır.
        private readonly IUnitOfWork _unitOfWork;

        // Constructor (Yapıcı Metot): Manager oluşturulurken ihtiyaç duyduğu araçları (Bağımlılıkları) buraya enjekte ediyoruz.
        public TableCategoryManager(IGenericRepository<TableCategory> categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        // TÜM KATEGORİLERİ LİSTELEME
        public async Task<IDataResult<List<TableCategoryDto>>> GetAllCategoriesAsync()
        {
            // 1. Veritabanındaki tüm kategori kayıtlarını asenkron (programı dondurmadan) çekiyoruz.
            var categories = await _categoryRepository.GetAll().ToListAsync();

            // 2. MAPPING (Dönüştürme): Veritabanı nesnelerini (Entity), 
            // dış dünyaya (Frontend) göndereceğimiz güvenli paketlere (DTO) çeviriyoruz.
            var dtos = categories.Select(c => new TableCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToList();

            // 3. Başarılı sonucu ve veriyi geri döndürüyoruz.
            return new SuccessDataResult<List<TableCategoryDto>>(dtos, "Masa kategorileri listelendi.");
        }

        // YENİ KATEGORİ OLUŞTURMA
        public async Task<IResult> CreateCategoryAsync(TableCategoryCreateDto createDto)
        {
            // 1. GÜVENLİK KONTROLÜ: Aynı isimde başka bir kategori var mı? 
            // (Küçük/büyük harf duyarlılığını kaldırmak için ToLower() kullanıyoruz)
            var exists = await _categoryRepository.Where(c => c.Name.ToLower() == createDto.Name.ToLower()).AnyAsync();
            if (exists) return new ErrorResult("Bu isimde bir kategori zaten mevcut!");

            // 2. DTO'dan gelen veriyi, veritabanına kaydedebileceğimiz Entity formatına sokuyoruz.
            var category = new TableCategory
            {
                Name = createDto.Name,
                Description = createDto.Description,
                IsActive = true // Yeni eklenen kategori varsayılan olarak aktiftir.
            };

            // 3. Bellekteki (RAM) listeye ekliyoruz.
            await _categoryRepository.AddAsync(category);

            // 4. UnitOfWork ile değişikliği SQL veritabanına mühürlüyoruz.
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Yeni kategori başarıyla oluşturuldu.");
        }

        // KATEGORİ GÜNCELLEME
        public async Task<IResult> UpdateCategoryAsync(TableCategoryUpdateDto updateDto)
        {
            // 1. Güncellenmek istenen kaydı ID üzerinden veritabanında arıyoruz.
            var category = await _categoryRepository.GetByIdAsync(updateDto.Id);
            if (category == null) return new ErrorResult("Kategori bulunamadı.");

            // 2. Bulunan kaydın bilgilerini DTO'dan gelen yeni bilgilerle değiştiriyoruz.
            category.Name = updateDto.Name;
            category.Description = updateDto.Description;
            category.IsActive = updateDto.IsActive;

            // 3. Değişikliği bildirip veritabanına kaydediyoruz.
            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Kategori bilgileri güncellendi.");
        }

        // KATEGORİ SİLME
        public async Task<IResult> DeleteCategoryAsync(int id)
        {
            // 1. INCLUDE KULLANIMI: Kategoriyi çekerken, ona bağlı "Masa" listesini de hafızaya getiriyoruz.
            // Çünkü boş olmayan bir kategoriyi silmek mantık hatasına yol açar.
            var category = await _categoryRepository.GetAll()
                .Include(c => c.Tables)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return new ErrorResult("Kategori bulunamadı.");

            // 2. KRİTİK GÜVENLİK: Eğer bu kategoriye bağlı masalar varsa silme işlemini REDDET.
            // Önce masaların kategorisinin değiştirilmesi veya silinmesi gerekir.
            if (category.Tables.Any())
            {
                return new ErrorResult("Bu kategoriye kayıtlı masalar var! Önce masaları başka bir kategoriye taşıyın.");
            }

            // 3. Güvenlikten geçtiyse sil ve kaydet.
            _categoryRepository.Delete(category);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Kategori sistemden tamamen silindi.");
        }
    }
}