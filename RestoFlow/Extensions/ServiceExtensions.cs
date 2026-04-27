using Business.Abstracts;
using Business.Concretes;
using Business.Mappings;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Data.Contexts;
using Data.Repositories;
using Data.UnitOfWorks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace WebAPI.Extensions
{
    // Bu sınıfın 'static' olmasının sebebi: Proje başlarken (Program.cs içinde) 
    // doğrudan çağrılabilmesi ve hafızada tek bir kopyasının yaşaması içindir.
    public static class ServiceExtensions
    {
        // 'this IServiceCollection services': Bu ifade bir "Extension (Genişletme)" metodudur.
        // Program.cs içindeki 'builder.Services' nesnesine sanki onun kendi özelliğiymiş gibi
        // 'AddInfrastructure' adında yeni bir yetenek kazandırır.
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ====================================================================
            // 1. VERİTABANI BAĞLANTISI (Entity Framework Core)
            // ====================================================================
            // Sisteme diyoruz ki: "Veritabanı işlemleri için AppDbContext sınıfını kullan.
            // Bu veritabanı bir SQLite veritabanıdır ve adresi de appsettings.json içindeki
            // 'DefaultConnection' satırında yazmaktadır."
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

            // ====================================================================
            // 2. BAĞIMLILIK ENJEKSİYONU (Dependency Injection - DI)
            // ====================================================================
            // Sisteme "Kim benden X arayüzünü isterse, ona Y sınıfını ver" diyoruz.
            // 'AddScoped': Her yeni HTTP isteği (Request) geldiğinde bu sınıftan 
            // yepyeni bir tane üretilir. İstek bitince de (Response dönünce) hafızadan silinir.

            // Veritabanı sorgularının yapıldığı yerler:
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>(); // Tüm işlemleri tek bir kerede kaydetmek (Commit) için.

            // İş kurallarımızın (Business Logic) çalıştığı yerler:
            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<IAuthService, AuthManager>();
            services.AddScoped<IOrderService, OrderManager>();
            services.AddScoped<ITableService, TableManager>();
            services.AddScoped<IPaymentService, PaymentManager>();
            services.AddScoped<ITableCategoryService, TableCategoryManager>();

            // ====================================================================
            // 3. AUTOMAPPER KAYDI (Veri Taşıyıcıları)
            // ====================================================================
            // Kullanıcıdan gelen DTO'ları (Örn: ProductDto) veritabanı nesnelerine (Örn: Product)
            // veya tam tersine çeviren kütüphaneyi devreye alıyoruz.
            // 'GeneralMapping': Hangi DTO'nun hangi nesneye dönüşeceğinin kurallarını yazdığımız dosya.
            services.AddAutoMapper(config =>
            {
                config.AddProfile<GeneralMapping>();
            });

            // ====================================================================
            // 4. KİMLİK YÖNETİMİ (ASP.NET Core Identity)
            // ====================================================================
            // Sistemin varsayılan kullanıcı tablosunun 'AppUser', rol tablosunun 'IdentityRole' olduğunu belirtiyoruz.
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                // Şifre kurallarını belirliyoruz. Geliştirme aşamasında bizi yormaması için 
                // "Büyük harf olsun, sembol olsun" gibi zorunlulukları şimdilik (false) kapattık.
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            // Bu kimlik bilgilerinin hafızada değil, bizim belirlediğimiz 
            // SQLite veritabanında (AppDbContext) saklanacağını söylüyoruz.
            .AddEntityFrameworkStores<AppDbContext>()
            // Şifre sıfırlama, e-posta onaylama gibi işlemler için gereken rastgele kod (Token) üreticileri.
            .AddDefaultTokenProviders();

            // ====================================================================
            // 5. GÜVENLİK VE YETKİLENDİRME (JWT - JSON Web Token)
            // ====================================================================
            // NOT: Bu bloğun Identity'den SONRA yazılması çok kritiktir!

            services.AddAuthentication(options =>
            {
                // API'ye şunu diyoruz: "Biri kapıya geldiğinde onun kimliğini Cookie (Çerez) 
                // veya başka bir yöntemle değil, SADECE VE SADECE JWT (Bearer Token) ile kontrol et!"
                // Bu ayar bizi yetkisiz durumlarda 404 (Sayfa bulunamadı) hatasından kurtarıp 
                // doğru hata olan 401 (Yetkisiz) hatasına yönlendirir.
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Gelen Token'ın (yaka kartının) sahte olup olmadığını nasıl anlayacağımızın kuralları:
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Token'ı üreten (Issuer) kurum doğru mu? (RestoFlowAPI)
                    ValidateIssuer = true,

                    // Token'ı kullanacak (Audience) kitle doğru mu? (RestoFlowUsers)
                    ValidateAudience = true,

                    // Token'ın son kullanma tarihi (Örn: 24 saat) geçmiş mi?
                    ValidateLifetime = true,

                    // Token'ın altındaki imza (Mühür) gerçek mi? (En önemli güvenlik adımı)
                    ValidateIssuerSigningKey = true,

                    // Peki bu kontrolleri yaparken nelere bakılacak? 
                    // appsettings.json içindeki 'JwtSettings' bilgilerini getirip sisteme veriyoruz:
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],

                    // İmza kontrolü için gizli anahtarımızı (SecretKey) kriptolayıp (UTF8 byte dizisine çevirip) veriyoruz.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]))
                };
            });
            // ====================================================================
            // 6. YETKİLENDİRME POLİTİKALARI (Discord Tarzı Yetki Sistemi)
            // ====================================================================
            // Bu blok, veritabanındaki "Permission" Claim'leri ile Controller'daki
            // [Authorize(Policy = ...)] etiketlerini birbirine bağlayan köprüdür.

            services.AddAuthorization(options =>
            {
                // Reflection (Yansıma) kullanarak Permissions sınıfı içindeki tüm kategorileri buluyoruz.
                // Orders, Payments, Inventory gibi alt sınıfların içindeki tüm 'const' metinleri topluyoruz.
                var allPermissions = typeof(Core.Constants.Permissions).GetNestedTypes()
                    .SelectMany(x => x.GetFields().Select(f => f.GetValue(null).ToString()))
                    .ToList();

                foreach (var permission in allPermissions)
                {
                    // Dinamik Politika Oluşturma:
                    // Her bir yetki ismi (Örn: "Permissions.Operations.CreateOrder") için bir kural koyuyoruz.
                    // Kural şu: "Eğer kullanıcının Token'ında bu isimde bir 'Permission' varsa geçebilir."
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim("Permission", permission));
                }
            });
        }
    }
}