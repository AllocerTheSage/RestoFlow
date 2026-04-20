// Dosya Yolu: RestoFlow.API/Controllers/TablesController.cs
using Business.Abstracts;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ToListAsync metodunun çalışması için gereklidir

namespace RestoFlow.API.Controllers
{
    // Dış dünyadan (Swagger veya Garson Tabletinden) gelen isteklerin
    // "http://localhost:5000/api/tables" adresine yönlendirilmesini sağlar.
    [Route("api/[controller]")]

    // Bu sınıfın bir API kumandası olduğunu belirtir (Gelen hatalı verileri otomatik engeller).
    [ApiController]
    public class TablesController : ControllerBase
    {
        // ==========================================
        // ASİSTANLAR (Bağımlılıklar)
        // ==========================================

        // Veritabanındaki 'Tables' tablosuna yeni bir satır eklemek veya okumak için kullanacağımız depo.
        private readonly IGenericRepository<Table> _tableRepository;

        // Yapılan değişiklikleri (kayıtları) veritabanına tek seferde "kalıcı" olarak kaydetmek (Commit) için araç.
        private readonly IUnitOfWork _unitOfWork;

        // Dependency Injection (Bağımlılık Enjeksiyonu): 
        // Sistem çalışırken (uygulama ayağa kalkarken) bu asistanları Controller'a otomatik olarak tahsis eder.
        public TablesController(IGenericRepository<Table> tableRepository, IUnitOfWork unitOfWork)
        {
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
        }

        // ==========================================
        // 1. YENİ MASA EKLEME UCU (ENDPOINT)
        // ==========================================
        // [HttpPost]: Dışarıdan sistemimize "Yeni bir veri gönderileceği/yazılacağı" zaman kullanılır.
        // Garson veya patron tabletten "Masa Ekle" butonuna bastığında bu metot tetiklenir.
        [HttpPost("add")]
        public async Task<IActionResult> AddTable(string tableNumber, int capacity)
        {
            // Gelen bilgilere göre C# hafızasında yeni bir masa objesi (taslağı) oluşturuyoruz.
            var table = new Table
            {
                TableNumber = tableNumber, // Örn: "Masa-1" veya "VIP-Odasi"
                Capacity = capacity,       // Örn: 4 (kişilik)

                // Sisteme ilk kez eklenen bir masa fiziksel olarak boş demektir.
                // Bu yüzden durumunu varsayılan olarak "Empty" (Yeşil/Boş) olarak ayarlıyoruz.
                Status = Core.Concretes.Enums.TableStatus.Empty
            };

            // Hazırladığımız bu masa taslağını veritabanı deposuna ekliyoruz.
            await _tableRepository.AddAsync(table);

            // Ve 'UnitOfWork' ile değişiklikleri SQLite dosyasına kalıcı olarak yazıyoruz.
            await _unitOfWork.SaveChangesAsync();

            // İşlem başarılı olursa, dış dünyaya "200 OK" koduyla birlikte bilgi mesajı dönüyoruz.
            // Ayrıca oluşturulan masanın veritabanındaki matematiksel ID'sini (TableId) de veriyoruz.
            return Ok(new { Message = $"{tableNumber} başarıyla sisteme eklendi.", TableId = table.Id });
        }

        // ==========================================
        // 2. TÜM MASALARI LİSTELEME UCU (ENDPOINT)
        // ==========================================
        // [HttpGet]: Sistemden "Veri okumak / listelemek" istediğimizde kullanılır.
        // Ana ekranda restoranın krokisini çizerken tüm masaları getirmek için bu metot çalışır.
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllTables()
        {
            // Masalar deposuna gidip, içerideki tüm kayıtları (GetAll) çekip bir listeye (ToListAsync) dönüştürüyoruz.
            var tables = await _tableRepository.GetAll().ToListAsync();

            // Bulunan listeyi dış dünyaya (Ekrana / Swagger'a) gönderiyoruz.
            return Ok(tables);
        }
    }
}