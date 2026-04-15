using Core.Constants;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Data.Seeds
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndPermissionsAsync(RoleManager<IdentityRole> roleManager)
        {
            // 1. ROLLERİ TANIMLA
            string[] roleNames = { "Admin", "Garson", "Mutfak" };

            foreach (var roleName in roleNames)
            {
                // Eğer rol veritabanında yoksa oluştur
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. YETKİLERİ DAĞIT
            // Admin Rolü: Tüm yetkilere sahip olur
            var adminRole = await roleManager.FindByNameAsync("Admin");
            await AddAllPermissionsToRole(roleManager, adminRole);

            // Garson Rolü: Sadece operasyonel ve temel masa yetkileri
            var garsonRole = await roleManager.FindByNameAsync("Garson");
            await AddGarsonPermissions(roleManager, garsonRole);

            // Mutfak Rolü: Stok ve sipariş durum yetkileri
            var mutfakRole = await roleManager.FindByNameAsync("Mutfak");
            await AddMutfakPermissions(roleManager, mutfakRole);
        }

        private static async Task AddAllPermissionsToRole(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Permissions.cs içindeki her şeyi Admin'e ekler
            var allPermissions = typeof(Permissions).GetNestedTypes()
                .SelectMany(x => x.GetFields().Select(f => f.GetValue(null).ToString()))
                .ToList();

            foreach (var permission in allPermissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }

        private static async Task AddGarsonPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Garsonun yapabileceği operasyonel işlemler:
            var permissions = new List<string>
    {
        Permissions.Operations.ViewStockCount,   // Stok miktarını görsün
        Permissions.Operations.CreateOrder,      // Sipariş alsın
        Permissions.Operations.TrackOrderStatus, // Hazırlanma durumunu izlesin
        Permissions.TableManagement.DeleteProduct, // Siparişi iptal etsin (ürünü silsin)
        // HATA BURADAYDI: Doğru yol Permissions.Operations.View veya senin Orders altında tuttuğun isim olmalı
        // Senin son Permissions.cs haline göre 'Permissions.TableManagement.DeleteProduct' gibi bir yetki mi verelim yoksa 'View' mı?
        // Eğer masaları görmesini istiyorsan, son listemize göre Finance dışındaki View yetkilerini ekleyebiliriz.
    };

            foreach (var permission in permissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }

        private static async Task AddMutfakPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            // Mutfağın yapabileceği işlemler:
            var permissions = new List<string>
            {
                Permissions.Operations.ConfirmAndDeductStock, // Stoktan düşsün (-) butonu
                Permissions.Operations.ToggleAvailability,    // Ürünü kapatsın (Üstünü çizme)
                Permissions.Operations.TrackOrderStatus       // Sipariş durumunu güncellesin
            };

            foreach (var permission in permissions)
                await AddClaimIfNotExists(roleManager, role, permission);
        }

        private static async Task AddClaimIfNotExists(RoleManager<IdentityRole> roleManager, IdentityRole role, string permission)
        {
            var allClaims = await roleManager.GetClaimsAsync(role);
            if (!allClaims.Any(a => a.Type == "Permission" && a.Value == permission))
            {
                await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }
    }
}