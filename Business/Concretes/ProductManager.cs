using AutoMapper;
using Business.Abstracts;
using Business.DTOs.ProductDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;

namespace Business.Concretes
{
    public class ProductManager : IProductService
    {
        private readonly IGenericRepository<Core.Concretes.Entities.Product> _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        // Constructor Injection: İhtiyacımız olan araçları dışarıdan alıyoruz.
        public ProductManager(IGenericRepository<Core.Concretes.Entities.Product> productRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Tüm ürünleri getirir
        public async Task<IDataResult<List<ProductDto>>> GetAllAsync()
        {
            var products = await _productRepository.GetAll().ToListAsync();
            var productDtos = _mapper.Map<List<ProductDto>>(products);
            return new SuccessDataResult<List<ProductDto>>(productDtos, "Ürünler başarıyla listelendi.");
        }

        // Id'ye göre tek bir ürün getirir
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

        // Yeni ürün ekleme
        public async Task<IResult> AddAsync(ProductDto productDto)
        {
            // 1. DTO'yu veritabanı nesnesine (Entity) çeviriyoruz
            var product = _mapper.Map<Core.Concretes.Entities.Product>(productDto);

            // 2. Repository üzerinden ekleme emrini veriyoruz
            await _productRepository.AddAsync(product);

            // 3. UnitOfWork ile değişikliği veritabanına fiziksel olarak yazıyoruz
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Ürün başarıyla eklendi.");
        }

        // Ürün güncelleme
        public async Task<IResult> UpdateAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Core.Concretes.Entities.Product>(productDto);
            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync();
            return new SuccessResult("Ürün başarıyla güncellendi.");
        }

        // Ürün silme
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