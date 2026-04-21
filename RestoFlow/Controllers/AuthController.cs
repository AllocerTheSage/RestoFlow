using Business.Abstracts;
using Business.DTOs.AuthDtos;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    // 1. [Route]: Bu API'nin internet üzerindeki adresini belirler. 
    // "[controller]" yazan yere otomatik olarak sınıfın adı (Auth) gelir. 
    // Yani dışarıdan bu kapıya gelmek isteyen biri şu adresi kullanacak: localhost:port/api/Auth
    [Route("api/[controller]")]

    // 2. [ApiController]: Sisteme "Bu sınıf bir web sayfası (HTML) değil, bir API'dir" der.
    // Çok güzel bir yeteneği vardır: Dışarıdan gelen verilerin (DTO'ların) boş olup olmadığını otomatik kontrol eder.
    [ApiController]
    public class AuthController : ControllerBase
    {
        // 3. BAĞIMLILIK ENJEKSİYONU (Dependency Injection)
        // Burada doğrudan 'AuthManager' yazmıyoruz, 'IAuthService' (Arayüz) yazıyoruz. 
        // Böylece API, beynin nasıl çalıştığını bilmez, sadece ona "Kayıt yap" veya "Giriş yap" der. Bu bir "Pro" kuralıdır.
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // --------------------------------------------------------
        // KAYIT OLMA UCU (Endpoint)
        // [HttpPost]: Veritabanına yeni bir veri YAZILACAĞI için GET değil, POST kullanıyoruz.
        // Tam Adres: POST /api/Auth/register
        // --------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            // İsteği aldık, beynimize (AuthManager) gönderdik ve sonucunu bekliyoruz.
            var result = await _authService.RegisterAsync(registerDto);

            if (result.Success)
            {
                // İşlem başarılıysa HTTP 200 (OK) kodunu dönüyoruz.
                // Bu kod, karşı taraftaki uygulamaya "İşlemin sorunsuz gerçekleşti" demek için evrensel bir standarttır.
                return Ok(result);
            }

            // Eğer şifre kurallara uymazsa veya o mail adresi zaten varsa:
            // HTTP 400 (Bad Request - Kötü İstek) dönüyoruz. Karşı tarafa "Bana gönderdiğin verilerde sorun var" diyoruz.
            return BadRequest(result);
        }

        // --------------------------------------------------------
        // GİRİŞ YAPMA UCU (Endpoint)
        // Tam Adres: POST /api/Auth/login
        // --------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Kullanıcı adı ve şifreyi beyne gönderip şifreyi doğruluyoruz.
            var result = await _authService.LoginAsync(loginDto);

            if (result.Success)
            {
                // ŞİFRE DOĞRUYSA:
                // HTTP 200 (OK) dönüyoruz. Asıl sihir burada; result objesinin içinde 
                // bizim ürettiğimiz o dijital yaka kartı (JWT Token) var ve karşı tarafa teslim ediliyor.
                return Ok(result);
            }

            // ŞİFRE VEYA KULLANICI ADI YANLIŞSA:
            // Kurumsal API'lerde şifre hatalarına asla BadRequest dönülmez. 
            // Bunun yerine her zaman HTTP 401 (Unauthorized - Yetkisiz) dönülür. 
            // Bu, "Bu kapıdan geçmek için geçerli bir kimliğin yok" demektir.
            return Unauthorized(result);
        }
        // Personelleri Listeleme Ucu
        [HttpGet("users")]
        // [Authorize(Roles = "Admin")] // İleride sadece Adminler görebilsin diye burayı açabiliriz.
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _authService.GetAllUsersAsync();
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // Yetki Güncelleme Ucu
        [HttpPost("update-permissions")]
        public async Task<IActionResult> UpdatePermissions([FromBody] UpdatePermissionDto updateDto)
        {
            var result = await _authService.UpdateUserPermissionsAsync(updateDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // Personel Silme Ucu
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _authService.DeleteUserAsync(id);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
    }
}