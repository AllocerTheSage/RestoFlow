using Business.DTOs.AuthDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface IAuthService
    {
        // Kullanıcı kayıt işlemi (Sadece başarılı/başarısız döner)
        Task<IResult> RegisterAsync(RegisterDto registerDto);

        // Kullanıcı giriş işlemi (Başarılı olursa geriye string olarak Token döner)
        Task<IDataResult<string>> LoginAsync(LoginDto loginDto);
    }
}