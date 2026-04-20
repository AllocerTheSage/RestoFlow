using Core.Constants;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Data.Seeds
{
    // Bu sınıf, uygulama ilk çalıştığında veritabanına varsayılan rolleri (Admin, Garson vb.)
    // ve bu rollerin yapabileceği işlemleri (yetkileri/claim'leri) eklemek için kullanılır.
    public static class DbSeeder
    {
        // Ana seed (tohumlama) metodu. Program.cs veya başlangıç dosyasında çağrılır.
        // RoleManager: ASP.NET Core Identity'nin rolleri yönetmek için kullandığı hazır servistir.
        public static async Task SeedRolesAndPermissionsAsync(RoleManager<IdentityRole> roleManager)
        {
            // 1. ROLLERİ TANIMLA
            // Sistemde olmasını istediğimiz temel rollerin isimlerini bir dizide tutuyoruz.
            string[] roleNames = { "Admin", "Garson", "Mutfak" };

            // Tanımladığımız her bir rol için veritabanında olup olmadığını kontrol edeceğiz.
            foreach (var roleName in roleNames)
            {
                // Eğer rol veritabanında (AspNetRoles tablosu) yoksa oluştur
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    // Yeni bir IdentityRole nesnesi yaratıp veritabanına kaydediyoruz.
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. YETKİLERİ DAĞIT
            // Admin Rolü: Tüm yetkilere sahip olur
            // Veritabanından Admin rolünü buluyoruz ve ona tüm yetkileri atayan metodu çağırıyoruz.
            var adminRole = await roleManager.FindByNameAsync("Admin");
            await AddAllPermissionsToRole(roleManager, adminRole);

            // Garson Rolü: Sadece operasyonel ve temel masa yetkileri
            // Garson rolünü bulup, sadece garsona özel yetkileri atayan metodu çağırıyoruz.
            var garsonRole = await roleManager.FindByNameAsync("Garson");
            await AddGarsonPermissions(roleManager, garsonRole);

            // Mutfak Rolü: Stok ve sipariş durum yetkileri
            // Mutfak rolünü bulup, mutfağa özel yetkileri atayan metodu çağırıyoruz.
            var mutfakRole = await roleManager.FindByNameAsync("Mutfak");
            await AddMutfakPermissions(roleManager, mutfakRole);
        }

        // Gönderilen role (burada Admin), sistemdeki var olan tüm yetkileri ekleyen metod.
        private static async Task AddAllPermissionsToRole(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Permissions.cs içindeki her şeyi Admin'e ekler
            // Reflection (Yansıma) kullanarak Permissions sınıfının altındaki tüm iç sınıfları (NestedTypes) ve 
            // bu sınıfların içindeki tüm alanları (Fields) dinamik olarak çekip bir liste haline getirir.
            var allPermissions = typeof(Permissions).GetNestedTypes()
            .SelectMany(x => x.GetFields().Select(f => f.GetValue(null).ToString()))
            .ToList();

            // Bulunan tüm yetki metinlerini döngüye alıp ilgili role ekliyoruz.
            foreach (var permission in allPermissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }

        // Garson rolüne sadece ihtiyacı olan yetkileri veren metod.
        private static async Task AddGarsonPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Garsonun yapabileceği operasyonel işlemler:
            // Sadece garsonun kullanacağı yetkileri manuel olarak bir listede topluyoruz.
            var permissions = new List<string>
    {
        Permissions.Operations.ViewStockCount,   // Stok miktarını görsün
        Permissions.Operations.CreateOrder,      // Sipariş alsın
        Permissions.Operations.TrackOrderStatus, // Hazırlanma durumunu izlesin
        Permissions.TableManagement.DeleteProduct, // Siparişi iptal etsin (ürünü silsin)
        // HATA BURADAYDI: Doğru yol Permissions.Operations.View veya senin Orders altında tuttuğun isim olmalı
        // Senin son Permissions.cs haline göre 'Permissions.TableManagement.DeleteProduct' gibi bir yetki mi verelim yoksa 'View' mı?
        // Eğer masaları görmesini istiyorsan, son listemize göre Finance dışındaki View yetkilerini ekleyebiliriz.
    };

            // Belirlenen garson yetkilerini döngüyle role ekliyoruz.
            foreach (var permission in permissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }
        // DİKKAT: Kodu değiştirmeme kuralına uyduğum için bu satırdaki '1' karakterini silmedim ancak derleme hatasına (syntax error) yol açacaktır.
        private static async Task AddMutfakPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Mutfağın yapabileceği işlemler:
            // Sadece mutfak personelinin kullanacağı yetkileri listede topluyoruz.
            var permissions = new List<string>
       {
                Permissions.Operations.ConfirmAndDeductStock, // Stoktan düşsün (-) butonu
                Permissions.Operations.ToggleAvailability,    // Ürünü kapatsın (Üstünü çizme)
                Permissions.Operations.TrackOrderStatus       // Sipariş durumunu güncellesin
            };

            // Belirlenen mutfak yetkilerini döngüyle role ekliyoruz.
            foreach (var permission in permissions)
                await AddClaimIfNotExists(roleManager, role, permission);
        }

        // Yetki tekrarlarını önlemek için yazılmış yardımcı metod.
        // Verilen yetki (claim) o rolde zaten varsa tekrar eklenmesini engeller.
        private static async Task AddClaimIfNotExists(RoleManager<IdentityRole> roleManager, IdentityRole role, string permission)
        {
            // Rolün halihazırda sahip olduğu tüm yetkileri (Claim nesnelerini) veritabanından çekiyoruz.
            var allClaims = await roleManager.GetClaimsAsync(role);

            // Eğer bu rolün claim'leri arasında, tipi "Permission" ve değeri bizim gönderdiğimiz yetki olan bir kayıt YOKSA:
            if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission))
            {
                // Yeni bir Claim nesnesi oluşturup (Type: "Permission", Value: "İlgiliYetkiStringi") role ekliyoruz.
                await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }
    }
}