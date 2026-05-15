using Business.Abstracts;
using Business.DTOs.AuthDtos;
using Core.Abstracts;
using Core.Concretes.Entities;
using Core.Concretes.Enums; // LogType için eklendi
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        // ==========================================
        // CASUSUMUZ GERİ DÖNDÜ (Patron Logları)
        // ==========================================
        private readonly ILogService _logService;

        public AuthManager(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogService logService) // İçeri alındı
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logService = logService; // Eşitlendi
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
                // 1. GÜVENLİK AĞI: Veritabanında roller yoksa önce onları garantiye alıyoruz
                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Member"));
                }
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // ==========================================
                // YENİ VE KUSURSUZ MANTIK: İLK GELEN PATRON OLUR!
                // ==========================================

                // Sistemdeki mevcut "Admin" yetkisine sahip kişileri sayıyoruz.
                var existingAdmins = await _userManager.GetUsersInRoleAsync("Admin");

                if (existingAdmins.Count == 0)
                {
                    // Sistemde HİÇ admin yok. Bu kayıt olan kişi İLK kişi. Ona Admin yetkisi veriyoruz.
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    // Sistemde zaten bir Admin var! O yüzden bu kayıt olan kişiye SADECE Member yetkisi veriyoruz.
                    // (Kullanıcı adı "admin" veya "patron" olsa bile Member olarak kalacak, sistemi hackleyemeyecek).
                    await _userManager.AddToRoleAsync(user, "Member");
                }

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

            var token = await GenerateJwtTokenAsync(user);

            // ==========================================
            // CASUS İŞ BAŞINDA: BAŞARILI GİRİŞİ LOGLA
            // ==========================================
            await _logService.AddLogAsync(
                LogType.UserLogin,
                user.Id,
                $"{user.FirstName} {user.LastName} sisteme başarıyla giriş yaptı."
            );

            return new SuccessDataResult<string>(token, "Giriş başarılı.");
        }

        private async Task<string> GenerateJwtTokenAsync(AppUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                // İŞTE BÜYÜK DÜZELTME BURADA: user.UserName yerine user.Id yazdık!
                // Artık .NET bizim gerçek GUID değerimizi ezemeyecek. Siparişler çökmeyecek!
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));

                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

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
                    Role = roles.FirstOrDefault() ?? "Member",
                    Claims = claims.Select(c => c.Value).ToList()
                });
            }

            return new SuccessDataResult<List<UserListDto>>(userList, "Personeller başarıyla listelendi.");
        }

        public async Task<IResult> UpdateUserPermissionsAsync(UpdatePermissionDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(updateDto.UserId);
            if (user == null) return new ErrorResult("Personel bulunamadı.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(updateDto.Role))
            {
                if (!await _roleManager.RoleExistsAsync(updateDto.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(updateDto.Role));
                }
                await _userManager.AddToRoleAsync(user, updateDto.Role);
            }

            var currentClaims = await _userManager.GetClaimsAsync(user);
            await _userManager.RemoveClaimsAsync(user, currentClaims);

            var newClaims = updateDto.Claims.Select(claimValue => new Claim("Permission", claimValue)).ToList();
            await _userManager.AddClaimsAsync(user, newClaims);

            // ==========================================
            // CASUS: YETKİ DEĞİŞİMİNİ LOGLA
            // ==========================================
            await _logService.AddLogAsync(
                LogType.RoleChanged,
                null,
                $"Sistem Yetkilisi, {user.FirstName} {user.LastName} adlı personelin rolünü '{updateDto.Role}' olarak güncelledi."
            );

            return new SuccessResult("Personel yetkileri başarıyla güncellendi.");
        }

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