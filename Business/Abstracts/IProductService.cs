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

        // ====================================================================
        // MUTFAK VE OPERASYON MEKANİKLERİ (İletişimsiz İletişim Akışı)
        // ====================================================================

        // [MUTFAK] Sipariş hazırlandığında stok miktarını (StockQuantity) 1 eksiltir.
        // Controller'daki 'ConfirmAndDeductStock' yetkisine bağlı çalışır.
        Task<IResult> ReduceStockAsync(int id);

        // [MUTFAK/PATRON] Ürünün satış durumunu (IsAvailable) Aktif/Pasif olarak değiştirir.
        // Controller'daki 'ToggleAvailability' yetkisine bağlı çalışır.
        Task<IResult> ToggleAvailabilityAsync(int id);
    }
}