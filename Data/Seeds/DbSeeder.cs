using Core.Constants;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Data.Seeds
{
    // Bu sınıf, uygulama ilk çalıştığında veritabanına varsayılan rolleri (Admin, Garson, Member vb.)
    // ve bu rollerin yapabileceği işlemleri (yetkileri/claim'leri) eklemek için kullanılır.
    public static class DbSeeder
    {
        public static async Task SeedRolesAndPermissionsAsync(RoleManager<IdentityRole> roleManager)
        {
            // 1. ROLLERİ TANIMLA (Member rolü eklendi!)
            string[] roleNames = { "Admin", "Garson", "Mutfak", "Member" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. YETKİLERİ DAĞIT
            var adminRole = await roleManager.FindByNameAsync("Admin");
            if (adminRole != null) await AddAllPermissionsToRole(roleManager, adminRole);

            var garsonRole = await roleManager.FindByNameAsync("Garson");
            if (garsonRole != null) await AddGarsonPermissions(roleManager, garsonRole);

            var mutfakRole = await roleManager.FindByNameAsync("Mutfak");
            if (mutfakRole != null) await AddMutfakPermissions(roleManager, mutfakRole);

            // NOT: Member rolüne özel bir metot çağırmıyoruz. 
            // Sisteme yeni kayıt olanlar bu rolü alacak ve admin yetki verene kadar hiçbir işlem yapamayacak.
        }

        private static async Task AddAllPermissionsToRole(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            var allPermissions = typeof(Permissions).GetNestedTypes()
            .SelectMany(x => x.GetFields().Select(f => f.GetValue(null)?.ToString() ?? string.Empty))
            .ToList();

            foreach (var permission in allPermissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }

        private static async Task AddGarsonPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            var permissions = new List<string>
            {
                Permissions.Operations.ViewStockCount,
                Permissions.Operations.CreateOrder,
                Permissions.Operations.TrackOrderStatus,
                Permissions.TableManagement.DeleteProduct
            };

            foreach (var permission in permissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
        }

        private static async Task AddMutfakPermissions(RoleManager<IdentityRole> roleManager, IdentityRole role)
        {
            var permissions = new List<string>
            {
                Permissions.Operations.ConfirmAndDeductStock,
                Permissions.Operations.ToggleAvailability,
                Permissions.Operations.TrackOrderStatus
            };

            foreach (var permission in permissions)
            {
                await AddClaimIfNotExists(roleManager, role, permission);
            }
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