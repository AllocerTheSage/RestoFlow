using Business.Abstracts;
using Business.DTOs.PaymentDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    // Dışarıdan gelen kasa isteklerini "http://localhost:5000/api/payments" adresinde karşılar.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Güvenlik Duvarı: Giriş yapmamış hiç kimse (token'ı olmayan) kasaya yaklaşamaz!
    public class PaymentsController : ControllerBase
    {
        // Kasanın o akıllı beynini (PaymentManager) asistan olarak içeri alıyoruz.
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ====================================================================
        // ÖDEME ALMA UCU (KASİYER EKRANI)
        // ====================================================================
        // Kasiyer "200 TL Nakit Al" butonuna bastığında tabletin/bilgisayarın tetikleyeceği uç.
        // Dışarıdan POST isteği ile çağrılır.
        [HttpPost("receive")]
        // Eğer Permissions.cs dosyanın içinde kasaya özel bir yetkin varsa buraya ekleyebilirsin:
        // [Authorize(Policy = Permissions.Cashier.TakePayment)] 
        public async Task<IActionResult> ReceivePayment([FromBody] CreatePaymentDto paymentDto)
        {
            // Gelen kuryeyi (DTO) doğrudan bizim akıllı asistanımıza veriyoruz.
            // O gidip "Adisyon var mı? Borç bitti mi? Masa boşalmalı mı?" matematiklerini yapacak.
            var result = await _paymentService.ReceivePaymentAsync(paymentDto);

            // Eğer işlem başarılıysa kasiyere 200 OK ve "Ödeme alındı" mesajını dön.
            if (result.Success)
            {
                return Ok(result);
            }

            // Eğer adisyon bulunamazsa veya zaten kapalıysa 400 Bad Request dönüp kasiyeri uyar.
            return BadRequest(result);
        }
    }
}