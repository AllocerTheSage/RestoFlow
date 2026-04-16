using AutoMapper;
using Business.Abstracts;
using Business.DTOs.ProductDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // LOGLAMA İÇİN EKLENDİ

namespace Business.Concretes
{
    public class ProductManager : IProductService
    {
        private readonly IGenericRepository<Core.Concretes.Entities.Product> _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductManager> _logger; // LOGLAYICI TANIMLANDI

        // Constructor Injection
        public ProductManager(
            IGenericRepository<Core.Concretes.Entities.Product> productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ProductManager> logger) // DIŞARIDAN İSTENDİ
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger; // EŞLEŞTİRİLDİ
        }

        public async Task<IDataResult<List<ProductDto>>> GetAllAsync()
        {
            var products = await _productRepository.GetAll().ToListAsync();
            var productDtos = _mapper.Map<List<ProductDto>>(products);
            return new SuccessDataResult<List<ProductDto>>(productDtos, "Ürünler başarıyla listelendi.");
        }

        public async Task<IDataResult<ProductDto>> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return new ErrorDataResult<ProductDto>("Ürün bulunamadı.");
            }
            var productDto = _mapper.Map<ProductDto>(product);
            return new SuccessDataResult<ProductDto>(productDto);
        }

        public async Task<IResult> AddAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Core.Concretes.Entities.Product>(productDto);
            await _productRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Ürün veritabanına başarıyla yazıldıktan hemen sonra log atıyoruz
            _logger.LogInformation("Sisteme yeni bir ürün eklendi: {@Product}", productDto);

            return new SuccessResult("Ürün başarıyla eklendi.");
        }

        public async Task<IResult> UpdateAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Core.Concretes.Entities.Product>(productDto);
            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ürün güncellendi. ID: {Id}", productDto.Id);

            return new SuccessResult("Ürün başarıyla güncellendi.");
        }

        public async Task<IResult> DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return new ErrorResult("Silinecek ürün bulunamadı.");
            }

            _productRepository.Delete(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ürün sistemden silindi. ID: {Id}", id);

            return new SuccessResult("Ürün başarıyla silindi.");
        }

        // ====================================================================
        // MUTFAK VE OPERASYON MEKANİKLERİ (İletişimsiz İletişim Akışı)
        // ====================================================================

        // [MUTFAK] Mutfak personeli siparişi teslim ettiğinde (-) butonuna basar ve bu metot çalışır.
        public async Task<IResult> ReduceStockAsync(int id)
        {
            // 1. Önce ürünü veritabanından bul
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return new ErrorResult("Ürün bulunamadı.");
            }

            // 2. Stok kontrolü yap (Eksiye düşmesini engelle)
            if (product.StockQuantity > 0)
            {
                product.StockQuantity -= 1; // Stoktan 1 adet düş

                _productRepository.Update(product); // Güncellemeyi repository'e bildir
                await _unitOfWork.SaveChangesAsync(); // Değişikliği kaydet

                // 3. Kritik operasyonu logla (Kim bilir belki gün sonu raporunda lazım olur)
                _logger.LogInformation("Mutfak stok düşümü yaptı. Ürün ID: {ProductId}, Kalan Stok: {StockQuantity}", product.Id, product.StockQuantity);

                return new SuccessResult("Stok başarıyla güncellendi.");
            }

            _logger.LogWarning("Stok yetersiz uyarısı alındı. Ürün ID: {ProductId}", product.Id);
            return new ErrorResult("Uyarı: Stokta düşülecek ürün kalmadı!");
        }

        // [MUTFAK/PATRON] Ürün bittiğinde veya fırın bozulduğunda ürünü menüde "Satışa Kapalı" (Gri) yapar.
        public async Task<IResult> ToggleAvailabilityAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return new ErrorResult("Ürün bulunamadı.");
            }

            // 1. Durumu tersine çevir (True ise False, False ise True yapar)
            product.IsActive = !product.IsActive;

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // 2. Hangi duruma geçtiğini bul ve logla
            string durumMesaji = product.IsActive ? "Satışa Açıldı" : "Satışa Kapatıldı";
            _logger.LogInformation("Ürün satış durumu değiştirildi. Ürün ID: {ProductId}, Yeni Durum: {Durum}", product.Id, durumMesaji);

            return new SuccessResult($"Ürün durumu güncellendi: {durumMesaji}.");
        }
    }
}