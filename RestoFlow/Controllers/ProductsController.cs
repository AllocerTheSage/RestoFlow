using Business.Abstracts; // İş mantığı arayüzlerine (IProductService gibi) erişmek için
using Business.DTOs.ProductDtos; // Veri taşıma nesnelerine (ProductDto) erişmek için
using Core.Constants; // Permissions (Yetki isimleri) sınıfına erişmek için
using Microsoft.AspNetCore.Authorization; // [Authorize] ve [Policy] için
using Microsoft.AspNetCore.Mvc; // API Controller özelliklerini kullanabilmek için

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] -> Bu Controller'daki tüm kapılar genel olarak kilitli. 
    // Ancak her kapının kendine has bir "Politika" anahtarı olacak.
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        // DI (Dependency Injection): İhtiyacımız olan servisi dışarıdan alıyoruz.
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // ====================================================================
        // 1. ÜRÜN LİSTELEME (GARSON & MUTFAK & PATRON)
        // ====================================================================
        // [GARSON] Müşteriden sipariş alırken stok miktarını bu sayede görür.
        [HttpGet("getall")]
        [Authorize(Policy = Permissions.Operations.ViewStockCount)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _productService.GetAllAsync();
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // ====================================================================
        // 2. YENİ ÜRÜN EKLEME (SADECE PATRON/YÖNETİM)
        // ====================================================================
        // Menüye yeni bir yemek veya içecek ekleme yetkisi.
        [HttpPost("add")]
        [Authorize(Policy = Permissions.Administration.ManageMenu)]
        public async Task<IActionResult> Add(ProductDto productDto)
        {
            var result = await _productService.AddAsync(productDto);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // ====================================================================
        // 3. STOK EKSİLTME - EKSİLT (-) BUTONU (SADECE MUTFAK)
        // ====================================================================
        // [MUTFAK] "Ürünü hazırladım, verdim" dediği an stoktan düşer.
        // Garsonun ekranındaki sayı anında azalır (İletişimsiz İletişim).
        [HttpPatch("reduce-stock/{id}")]
        [Authorize(Policy = Permissions.Operations.ConfirmAndDeductStock)]
        public async Task<IActionResult> ReduceStock(int id)
        {
            var result = await _productService.ReduceStockAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // ====================================================================
        // 4. SATIŞA KAPATMA - ÜRETİM DURDURMA (SADECE MUTFAK & PATRON)
        // ====================================================================
        // [MUTFAK] "Pizza bitti" veya "Fırın bozuldu" dediğinde ürünü griye çeker.
        // Garsonun ekranında ürünün üstü çizilir.
        [HttpPatch("toggle-availability/{id}")]
        [Authorize(Policy = Permissions.Operations.ToggleAvailability)]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var result = await _productService.ToggleAvailabilityAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}