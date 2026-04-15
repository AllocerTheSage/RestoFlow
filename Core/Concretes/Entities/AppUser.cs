using Microsoft.AspNetCore.Identity;

namespace Core.Concretes.Entities
{
    // IdentityUser'dan miras alıyoruz. Bu sayede Id, UserName, Email, PasswordHash gibi özellikler otomatik gelecek.
    public class AppUser : IdentityUser
    {
        // İleride kendi projemize özel özellikler eklemek istersek buraya yazacağız.
        // Örn: public string FirstName { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

    }
}