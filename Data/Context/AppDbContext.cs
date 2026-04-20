using Core.Concretes.Entities;
using Microsoft.AspNetCore.Identity; // IdentityRole için gerekli
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Contexts
{
    // IdentityDbContext<AppUser, IdentityRole, string> kullanarak:
    // 1. AppUser: Kendi kullanıcı sınıfımız
    // 2. IdentityRole: Standart rol sınıfımız
    // 3. string: Kullanıcı ve Rol ID'lerinin tipi (Guid yerine string kullanıyoruz)
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        // Diğer DbSet tanımlarının (Products, Orders vb.) yanına bunu ekliyoruz:

        public DbSet<Table> Tables { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}