using Business.DTOs.OrderDtos;
using Core.Abstracts;
using Core.Concretes.Entities;

namespace Business.Abstracts
{
    public interface IOrderService
    {
        // Garson siparişi girdiğinde çalışacak metod.
        Task<IResult> CreateOrderAsync(OrderCreateDto orderDto, string waiterId);
        // Bekleyen siparişleri mutfak ekranı için getirir
        Task<IDataResult<List<Order>>> GetPendingOrdersAsync();
        Task<IDataResult<Order>> GetActiveOrderByTableIdAsync(int tableId);
        Task<IResult> DeliverOrderAsync(int orderId);

        // Siparişi "Hazır" durumuna getirir ve stoğu otomatik düşer
        Task<IResult> SetOrderReadyAsync(int orderId);

        // Ödeme alındıktan sonra siparişi "Tamamlandı" durumuna getirir.
        Task<IResult> CloseOrderAsync(int orderId);
        // Patron ekranı için: Sadece o gün tamamlanmış (Completed) siparişlerin toplam tutarını hesaplar.
        Task<IDataResult<decimal>> GetDailyRevenueAsync();
        // Bir siparişi, belirtilen bir sebeple iptal eder. Gerekirse stokları geri iade alır.
        Task<IResult> CancelOrderAsync(int orderId, string cancellationReason);
        // Belirli bir siparişteki, belirli bir ürünü "İkram" olarak işaretler ve fiyattan düşer.
        Task<IResult> MakeItemComplimentaryAsync(int orderId, int orderItemId);
        // Mevcut bir siparişe yeni ürünler ekler
        Task<IResult> AddItemsToOrderAsync(AddItemsToOrderDto addItemsDto);
        // Kasada siparişe (Adisyona) manuel indirim uygular.
        Task<IResult> ApplyDiscountAsync(int orderId, decimal discountAmount);
        // Müşteriyi ve adisyonu bir masadan başka bir masaya taşır
        Task<IResult> TransferTableAsync(TransferTableDto transferDto);
        // Adisyondan hatalı girilen veya iptal edilen tek bir ürün satırını siler
        Task<IResult> RemoveItemFromOrderAsync(int orderId, int orderItemId);
        // Siparişi hazırlık aşamasına geçirir
        Task<IResult> StartPreparationAsync(int orderId);
        Task<IDataResult<List<Order>>> GetAllActiveOrdersAsync();
        // Eski siparişleri getirir (örneğin, tamamlanmış veya iptal edilmiş siparişler)
        // Dışarıdan başlangıç ve bitiş tarihi alabilen, istenmezse boş bırakılabilen (?) sözleşmemiz
        Task<IDataResult<List<Order>>> GetPastOrdersAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}