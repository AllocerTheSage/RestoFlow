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
                await _userManager.AddToRoleAsync(user, "Member");

                return new SuccessResult("Kayıt başarılı. Artık giriş yapabilirsiniz.");
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
            // AuthManager.cs içindeki GenerateJwtTokenAsync metodunun ilgili kısmı:

            // 1. Temel Kimlik Bilgilerini Listeye Ekle
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                
                // İŞTE EKSİK OLAN SİHİRLİ SATIR BURASI!
                // Kullanıcının Adını ve Soyadını birleştirip Token'ın içine "Name" etiketiyle mühürlüyoruz.
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
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
        // 1. TÜM PERSONELLERİ GETİR
        public async Task<IDataResult<List<UserListDto>>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);

                userList.Add(new UserListDto
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "Member", // Varsayılan rol
                    Claims = claims.Select(c => c.Value).ToList() // Kullanıcının özel yetkileri
                });
            }

            return new SuccessDataResult<List<UserListDto>>(userList, "Personeller başarıyla listelendi.");
        }

        // 2. YETKİ VE ROL GÜNCELLE
        public async Task<IResult> UpdateUserPermissionsAsync(UpdatePermissionDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(updateDto.UserId);
            if (user == null) return new ErrorResult("Personel bulunamadı.");

            // A) Mevcut Rolleri Temizle ve Yeni Rolü Ekle
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(updateDto.Role))
            {
                // Rol veritabanında yoksa oluştur (Opsiyonel güvenlik)
                if (!await _roleManager.RoleExistsAsync(updateDto.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(updateDto.Role));
                }
                await _userManager.AddToRoleAsync(user, updateDto.Role);
            }

            // B) Mevcut Ekstra Yetkileri (Claims) Temizle ve Yenilerini Ekle
            var currentClaims = await _userManager.GetClaimsAsync(user);
            await _userManager.RemoveClaimsAsync(user, currentClaims);

            // JS'den gelen yetkileri (örn: "CancelOrder") veritabanına Claim olarak işle
            var newClaims = updateDto.Claims.Select(claimValue => new Claim("Permission", claimValue)).ToList();
            await _userManager.AddClaimsAsync(user, newClaims);

            return new SuccessResult("Personel yetkileri başarıyla güncellendi.");
        }

        // 3. PERSONEL SİL
        public async Task<IResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new ErrorResult("Personel zaten silinmiş veya bulunamadı.");

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return new SuccessResult("Personel sistemden tamamen silindi.");
            }

            return new ErrorResult("Personel silinirken bir hata oluştu.");
        }
    }
}