using Business.Abstracts;
using Business.DTOs.AuthDtos;
using Core.Abstracts;
using Core.Concretes.Entities;
using Core.Concretes.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Business.Concretes
{
    /// <summary>
    /// Kimlik doğrulama işlemlerinden sorumlu merkez sınıf.
    /// Kayıt olma, giriş yapma ve güvenli anahtar (Token) üretme işlerini yönetir.
    /// </summary>
    public class AuthManager : IAuthService
    {
        // UserManager: Microsoft'un Identity kütüphanesiyle gelen, veritabanındaki 
        // kullanıcı tablosuyla (AspNetUsers) konuşmamızı sağlayan devasa bir araçtır.
        private readonly UserManager<AppUser> _userManager;

        // IConfiguration: appsettings.json dosyasındaki "SecretKey" gibi hassas ayarları okumak için kullanılır.
        private readonly IConfiguration _configuration;

        public AuthManager(UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Yeni bir kullanıcıyı sisteme kaydeder.
        /// </summary>
        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            // 1. DTO'dan gelen verilerle yeni bir kullanıcı (Entity) oluşturuyoruz
            var user = new AppUser
            {
                FirstName = registerDto.FirstName, // DTO'dan gelen Adı bağladık
                LastName = registerDto.LastName,   // DTO'dan gelen Soyadı bağladık
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            // 2. Güvenli Kayıt İşlemi:
            // Identity burada şifreyi ASLA açık metin (plain text) olarak kaydetmez.
            // Arka planda PBKDF2 gibi algoritmalarla şifreyi "Hash"leyerek (anlamsız bir diziye çevirerek) saklar.
            // Örnek: "123456" -> "AQAAAAEAACcQAAAAEG..."
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                return new SuccessResult("Kullanıcı başarıyla kaydedildi.");
            }

            // Hata Durumu: Şifre çok kısa olabilir, büyük harf eksik olabilir veya kullanıcı adı alınmış olabilir.
            // Identity'den gelen tüm hata mesajlarını birleştirip geri döndürüyoruz.
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ErrorResult($"Kayıt başarısız: {errors}");
        }

        /// <summary>
        /// Kullanıcı bilgilerini doğrular ve başarılıysa giriş anahtarı (Token) verir.
        /// </summary>
        public async Task<IDataResult<string>> LoginAsync(LoginDto loginDto)
        {
            // 1. Kullanıcıyı Bulma:
            // Veritabanında bu kullanıcı isminde biri var mı?
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
            {
                // Güvenlik ipucu: "Kullanıcı adı veya şifre hatalı" demek daha güvenlidir (hangi bilginin yanlış olduğunu gizler).
                return new ErrorDataResult<string>("Kullanıcı bulunamadı.");
            }

            // 2. Şifre Doğrulama:
            // Veritabanındaki hash ile kullanıcının girdiği şifreyi Identity bizim yerimize karşılaştırır.
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                return new ErrorDataResult<string>("Şifre hatalı.");
            }

            // 3. Token Üretimi:
            // Kimlik doğrulandı, şimdi kullanıcıya "bu kartla sistemde gezebilirsin" diyoruz.
            var token = GenerateJwtToken(user);
            return new SuccessDataResult<string>(token, "Giriş başarılı.");
        }

        /// <summary>
        /// JSON Web Token (JWT) üreten yardımcı metot.
        /// Bu metot dijital bir mühür basar.
        /// </summary>
        private string GenerateJwtToken(AppUser user)
        {
            // appsettings.json'daki ayarları çekiyoruz.
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            // 1. İmza Hazırlığı:
            // SecretKey, bizim "mühür" anahtarımızdır. Bu anahtarı bilen biri ancak token'ın geçerliliğini kontrol edebilir.
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 2. Claims (Hak İddiaları):
            // Token'ın içine gömülecek "kimlik bilgileri"dir. 
            // Bu bilgiler şifrelenmez (Base64 ile kodlanır), herkes okuyabilir ama kimse değiştiremez (mühür bozulur).
            // Claim'leri pasaporttaki vizeler veya damgalar gibi düşünebilirsin.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName), // Kullanıcı adı (Subject)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Her token için benzersiz ID
                new Claim(ClaimTypes.NameIdentifier, user.Id), // Veritabanındaki Id
                // İleride buraya yetkileri ekleyeceğiz: new Claim(ClaimTypes.Role, "Admin")11
            };

            // 3. Token Oluşturma Özellikleri:
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"], // Bu token'ı kim üretti? (RestoFlowAPI)
                audience: jwtSettings["Audience"], // Bu token kimler için? (RestoFlowUser)
                claims: claims, // Kimlik bilgileri
                expires: DateTime.Now.AddDays(1), // Token ne kadar süre geçerli? (Örn: 24 saat)
                signingCredentials: credentials); // Dijital imzamız

            // Token'ı string (metin) formatına çevirip geri yolluyoruz.
            // Sonuç şuna benzer: "eyJhbGciOiJIUzI1NiIsInR5..."
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}