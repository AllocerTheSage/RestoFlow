using AutoMapper;
using Business.Abstracts;
using Business.DTOs.ProductDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // 1. LOGLAMA İÇİN EKLENDİ

namespace Business.Concretes
{
    public class ProductManager : IProductService
    {
        private readonly IGenericRepository<Core.Concretes.Entities.Product> _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductManager> _logger; // 2. LOGLAYICI TANIMLANDI

        // Constructor Injection
        public ProductManager(
            IGenericRepository<Core.Concretes.Entities.Product> productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ProductManager> logger) // 3. DIŞARIDAN İSTENDİ
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger; // 4. EŞLEŞTİRİLDİ
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

            // 5. İŞTE BURASI: Ürün veritabanına başarıyla yazıldıktan hemen sonra log atıyoruz
            _logger.LogInformation("Sisteme yeni bir ürün eklendi: {@Product}", productDto);

            return new SuccessResult("Ürün başarıyla eklendi.");
        }

        public async Task<IResult> UpdateAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Core.Concretes.Entities.Product>(productDto);
            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
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
            return new SuccessResult("Ürün başarıyla silindi.");
        }
    }
}