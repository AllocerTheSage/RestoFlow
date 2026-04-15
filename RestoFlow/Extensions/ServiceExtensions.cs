using Business.Abstracts;
using Business.Concretes;
using Business.Mappings;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Data.Contexts;
using Data.Repositories;
using Data.UnitOfWorks;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Extensions
{
    public static class ServiceExtensions
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // SQLite Bağlantısı
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

            // --- Dependency Injection Kayıtları ---

            // Generic Repository Kaydı: 
            // 'T' ne gelirse gelsin (Product, Order vb.) GenericRepository'yi kullan diyoruz.
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // UnitOfWork Kaydı:
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Business Katmanı Servis Kaydı
            services.AddScoped<IProductService, ProductManager>();


            services.AddAutoMapper(config =>
            {
                config.AddProfile<GeneralMapping>();
            });
        }
    }
}