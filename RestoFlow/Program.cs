using Serilog;
using WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Yapılandırması
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// 2. Senin Servislerin
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. SWAGGER SERVİSLERİ (Muhtemelen burası silindi, bunları mutlaka ekle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- BUILD İŞLEMİ ---
var app = builder.Build();

// 4. Middleware (Ara Katman) Ayarları
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();