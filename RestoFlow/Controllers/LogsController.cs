using Business.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // GÜVENLİK KALKANI: Bu Controller'a ve içindeki hiçbir şeye Token'ında "Admin" rolü olmayan kimse erişemez!
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        // Sözleşmemizi (Interface) çağırıyoruz ki LogManager'ın yeteneklerini kullanalım.
        private readonly ILogService _logService;

        public LogsController(ILogService logService)
        {
            _logService = logService;
        }

        // GET: api/Logs/all
        // Frontend (log.js) verileri ekrana çizmek için bu URL'ye istek atacak.
        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs()
        {
            // LogManager'a diyoruz ki: "Bana bütün logları en yenisi en üstte olacak şekilde DTO formatında getir."
            var result = await _logService.GetAllLogsAsync();

            // Eğer işlem başarılıysa verileri 200 (OK) koduyla JavaScript'e yolla
            if (result.Success)
            {
                return Ok(result);
            }

            // Bir hata çıkarsa (Örn: Veritabanı kapalıysa) 400 koduyla hatayı yolla
            return BadRequest(new { success = false, message = result.Message });
        }
    }
}