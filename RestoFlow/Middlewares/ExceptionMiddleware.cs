using Core.CrossCuttingConcerns.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace WebAPI.Middlewares
{
    public class ExceptionMiddleware
    {
        // Bir sonraki adıma geçişi sağlayan delege (Trafik Polisi)
        private readonly RequestDelegate _next;

        // Hatanın detaylarını arka planda dosyaya (Serilog) yazacak asistan
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Dışarıdan gelen her HTTP isteği bu metodun içinden geçmek zorundadır!
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // Her şey yolundaysa isteği sisteme (Controller'lara) gönder
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // Eğer sistemin herhangi bir yerinde hata patlarsa, HAVADA YAKALA!

                // 1. Hatayı (tüm çıplaklığıyla) sadece patronun/yazılımcının göreceği Log dosyasına yaz
                _logger.LogError($"Beklenmeyen Bir Hata Yakalandı: {ex}");

                // 2. Müşteriye (Dış dünyaya) şık ve güvenli JSON paketini dön
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Cevabın bir web sayfası değil, JSON olduğunu belirtiyoruz
            context.Response.ContentType = "application/json";

            // Varsayılan olarak 500 (Sunucu İçi Hata) kodunu veriyoruz
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Dış dünyaya göndereceğimiz güvenli mesaj paketi
            var errorDetails = new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                // DİKKAT: Güvenlik gereği 'exception.Message' bilgisini dışarıya DÖNMÜYORUZ!
                // Hackerların kod yapımızı çözmemesi için standart bir mesaj veriyoruz.
                Message = "Sistemde anlık bir sorun oluştu. Lütfen daha sonra tekrar deneyiniz."
            };

            // JSON'ı ekrana (veya frontend'e) bas
            return context.Response.WriteAsync(errorDetails.ToString());
        }
    }
}