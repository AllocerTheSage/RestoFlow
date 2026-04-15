using Core.Concretes.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Contexts
{
    // IdentityDbContext kullanarak hem kendi tablolarımızı hem de 
    // hazır kullanıcı/rol tablolarını (AspNetUsers vb.) yönetiyoruz.
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        // Constructor: Veritabanı bağlantı ayarlarını (Connection String) dışarıdan alır.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veritabanında oluşacak tablolarımızı tanımlıyoruz:
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Tablolar oluşturulurken yapılacak özel ayarlar (İlişkiler, kısıtlamalar)
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Bu satır Data katmanındaki tüm 'IEntityTypeConfiguration' dosyalarını bulur ve uygular.
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}