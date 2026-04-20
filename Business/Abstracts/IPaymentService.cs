using Business.DTOs.PaymentDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface IPaymentService
    {
        // Parçalı veya tam ödeme alır, kalan tutarı hesaplar 
        // ve hesap kapandıysa masayı otomatik boşaltır.
        Task<IResult> ReceivePaymentAsync(CreatePaymentDto paymentDto);
    }
}