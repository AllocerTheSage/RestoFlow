using Business.DTOs.ProductDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface IProductService
    {
        // Tüm ürünleri liste olarak döner
        Task<IDataResult<List<ProductDto>>> GetAllAsync();

        // Id'ye göre tek bir ürün döner
        Task<IDataResult<ProductDto>> GetByIdAsync(int id);

        // Yeni ürün ekler
        Task<IResult> AddAsync(ProductDto productDto);

        // Ürün günceller
        Task<IResult> UpdateAsync(ProductDto productDto);

        // Ürün siler
        Task<IResult> DeleteAsync(int id);
    }
}