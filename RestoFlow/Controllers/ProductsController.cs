using Business.Abstracts; // İş mantığı arayüzlerine (IProductService gibi) erişmek için
using Business.DTOs.ProductDtos; // Veri taşıma nesnelerine (ProductDto) erişmek için
using Microsoft.AspNetCore.Mvc; // API Controller özelliklerini kullanabilmek için

namespace WebAPI.Controllers
{
    // [Route] -> Bu Controller'a nasıl ulaşılacağını belirler. 
    // [controller] ifadesi otomatik olarak sınıf ismini (Products) alır. 
    // Yani adres: https://localhost:7000/api/products olur.
    [Route("api/[controller]")]

    // [ApiController] -> Bu sınıfın bir Web API olduğunu sisteme bildirir.
    // Gelen verilerin otomatik doğrulanmasını (Validation) sağlar.
    [ApiController]
    public class ProductsController : ControllerBase
    {
        // _productService -> Bizim mutfağımız (Business). 
        // Veritabanı ve iş kuralları ile ilgili her şeyi bu servis üzerinden halledeceğiz.
        private readonly IProductService _productService;

        // CONSTRUCTOR (Yapıcı Metot)
        // Program çalıştığında, .NET bize otomatik olarak bir IProductService (ProductManager) nesnesi getirir.
        // Buna "Dependency Injection" diyoruz. Yani "ihtiyacım olanı bana dışarıdan ver" diyoruz.
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // [HttpGet("getall")] -> Tarayıcıdan veya dışarıdan "getall" diye bir istek geldiğinde burası çalışır.
        // Örnek Adres: api/products/getall
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            // Servise gidip "Bana tüm ürünleri getir" diyoruz.
            var result = await _productService.GetAllAsync();

            // Eğer mutfaktaki işlem başarılıysa (result.Success == true)
            if (result.Success)
            {
                // Kullanıcıya veriyi (Data) ve "200 OK" (Her şey yolunda) mesajını dönüyoruz.
                return Ok(result);
            }

            // İşlem başarısızsa (Örn: Veritabanına ulaşılamadı), kullanıcıya hata mesajını ve 400 kodunu dönüyoruz.
            return BadRequest(result);
        }

        // [HttpPost("add")] -> Dışarıdan yeni bir veri gönderildiğinde (Kaydetme işlemi) burası çalışır.
        // Örnek Adres: api/products/add
        [HttpPost("add")]
        public async Task<IActionResult> Add(ProductDto productDto)
        {
            // Kullanıcıdan gelen "productDto" paketini (Ad, Fiyat, Stok) alıp servise "Bunu ekle" diyoruz.
            var result = await _productService.AddAsync(productDto);

            // Ekleme işlemi başarılıysa "200 OK" ile sonucumuzu dönüyoruz.
            if (result.Success)
            {
                return Ok(result);
            }

            // Ekleme sırasında bir kural ihlali olduysa (Örn: Fiyat 0'dan küçükse) hata dönüyoruz.
            return BadRequest(result);
        }
    }
}