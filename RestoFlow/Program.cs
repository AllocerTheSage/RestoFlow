using Data.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Serilog;
using WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader();
        });
});

// 1. Serilog Yapılandırması
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// 2. Senin Servislerin
// JSON dönüştürücüsünün sonsuz döngüye girmesini (Object Cycle) engeller.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
// ====================================================================
// CORS (Cross-Origin Resource Sharing) AYARLARI
// ====================================================================
// Şimdilik geliştirme aşamasında olduğumuz için "AllowAll" (Herkese İzin Ver) politikası yazıyoruz.
// Canlıya alırken buraya sadece "www.restoflow.com" gibi kendi frontend adresimizi yazacağız.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()   // Hangi adresten gelirse gelsin (React, Angular, Mobil)
                         .AllowAnyMethod()   // Hangi metotla gelirse gelsin (GET, POST, PUT, DELETE)
                         .AllowAnyHeader();  // İçinde hangi başlık (Token vb.) olursa olsun kabul et
        });
});
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
// SWAGGER'A KART OKUYUCU (AUTHORIZE BUTONU) EKLEME
// builder.Services.AddSwaggerGen: Swagger dokümantasyon oluşturucusunu yapılandırmaya başlar.
builder.Services.AddSwaggerGen(c =>
{
    // Swagger arayüzünün en üstünde görünecek başlık ve versiyon bilgisi.
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RestoFlow API", Version = "v1" });

    // 1. GÜVENLİK TANIMI (Security Definition):
    // Swagger'a hangi kimlik doğrulama yöntemini desteklediğimizi öğretiyoruz.
    // Burada "Bearer" adında bir güvenlik şeması tanımlıyoruz.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", // HTTP isteğinin 'Header' kısmında hangi isimle görüneceği (Standart: Authorization).
        Type = SecuritySchemeType.ApiKey, // Güvenlik tipinin bir anahtar (Token) olduğunu belirtir.
        Scheme = "Bearer", // Kullanılan şemanın adı.
        BearerFormat = "JWT", // Anahtarın formatının JSON Web Token (JWT) olduğunu bildirir.
        In = ParameterLocation.Header, // Bu anahtarın HTTP isteğinin neresinde (Header/Başlık kısmında) taşınacağını söyler.

        // Kullanıcıya Swagger arayüzünde ne yapması gerektiğini anlatan açıklama metni:
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Aşağıdaki kutuya 'Bearer' yazıp bir boşluk bırakın ve ardından Token'ınızı yapıştırın.\r\n\r\nÖrnek: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...' "
    });

    // 2. GÜVENLİK GEREKSİNİMİ (Security Requirement):
    // Yukarıda tanımladığımız "Bearer" şemasının hangi API uçlarında geçerli olacağını belirliyoruz.
    // Bu kod bloğu, Swagger'daki TÜM metodların yanına bir 'asma kilit' simgesi koyar.
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, // Referans tipinin bir güvenlik şeması olduğunu belirtir.
                    Id = "Bearer" // Yukarıda 'AddSecurityDefinition' içinde verdiğimiz ID ile aynı olmalı.
                }
            },
            Array.Empty<string>() // Herhangi bir özel kapsam (scope) gerekmediğini belirtir.
        }
    });
});

// --- BUILD İŞLEMİ ---
var app = builder.Build();

// 4. Middleware (Ara Katman) Ayarları
app.UseSerilogRequestLogging();
// [KRİTİK EKLEME]: Hata Yakalayıcı burada olmalı. 
// Bu sayede aşağıdaki her şeyi (Swagger, Auth, Controllers) koruma altına alır.
app.UseMiddleware<WebAPI.Middlewares.ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();
// DİKKAT: CORS ayarı tam buraya, kimlik kontrolünden hemen önce gelmeli!
app.UseCors("AllowAll");
app.UseAuthentication(); // ÖNCE KİMLİK KONTROLÜ (Kimlik var mı?)
app.UseAuthorization();  // SONRA YETKİ KONTROLÜ (Bu odaya girmeye yetkisi var mı?)
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbSeeder.SeedRolesAndPermissionsAsync(roleManager);
}

app.Run();