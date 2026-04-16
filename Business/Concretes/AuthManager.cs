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
    public class AuthManager : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        // YENİ EKLENDİ: Rollerin içindeki yetkileri (Claimleri) okumak için RoleManager'ı çağırıyoruz.
        // NOT: Eğer projende 'AppRole' diye bir sınıf oluşturduysan 'IdentityRole' yazan yeri 'AppRole' yap.
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        // Constructor'a RoleManager'ı da ekledik (Dependency Injection)
        public AuthManager(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            var user = new AppUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                UserName = registerDto.Username,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                return new SuccessResult("Kullanıcı başarıyla kaydedildi.");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ErrorResult($"Kayıt başarısız: {errors}");
        }

        public async Task<IDataResult<string>> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
            {
                return new ErrorDataResult<string>("Kullanıcı bulunamadı.");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                return new ErrorDataResult<string>("Şifre hatalı.");
            }

            // DİKKAT: Artık yetkileri DB'den çekeceği için bu metodu "await" ile bekletiyoruz.
            var token = await GenerateJwtTokenAsync(user);
            return new SuccessDataResult<string>(token, "Giriş başarılı.");
        }

        // DİKKAT: Veritabanına bağlanacağı için metodu 'async Task<string>' olarak güncelledik.
        private async Task<string> GenerateJwtTokenAsync(AppUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 1. Temel Kimlik Bilgilerini Listeye Ekle
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            // ==============================================================
            // 2. İŞTE O EKSİK SİHİR BURASI: VERİTABANINDAN YETKİLERİ ÇEKME
            // ==============================================================

            // Kullanıcının rollerini buluyoruz (Örn: "Admin")
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                // Rolün kendisini Token'a ekle
                claims.Add(new Claim(ClaimTypes.Role, userRole));

                // O role ait veritabanındaki "Permission" yetkilerini bul
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var roleClaim in roleClaims)
                    {
                        // DB'deki ManageMenu, CreateOrder gibi yetkileri Token'a mühürle!
                        claims.Add(roleClaim);
                    }
                }
            }

            // (Opsiyonel) Eğer kullanıcıya özel (rolden bağımsız) yetkiler vermişsek onları da ekle
            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);
            // ==============================================================

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims, // Artık içi yetki dolu listemizi buraya veriyoruz!
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}