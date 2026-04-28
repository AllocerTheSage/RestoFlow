using Business.Abstracts;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Core.Constants; // Yetki kilidimiz için gerekli
using Microsoft.AspNetCore.Authorization; // [Authorize] için gerekli
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RestoFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Tüm masa işlemleri artık Token (Yaka kartı) istiyor.
    public class TablesController : ControllerBase
    {
        private readonly IGenericRepository<Table> _tableRepository;
        private readonly IUnitOfWork _unitOfWork;

        // ==========================================
        // YENİ ASİSTANIMIZ (Saha Haritası Beyni)
        // ==========================================
        private readonly ITableService _tableService;

        // Constructor'a 3. asistanı da ekledik
        public TablesController(
            IGenericRepository<Table> tableRepository,
            IUnitOfWork unitOfWork,
            ITableService tableService)
        {
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
            _tableService = tableService;
        }

        // ==========================================
        // 1. YENİ MASA EKLEME UCU (Mevcut Kodun)
        // ==========================================
        [HttpPost("add")]
        [Authorize(Roles = "Admin")] // SADECE PATRON/YÖNETİCİ MASA EKLEYEBİLİR!
        public async Task<IActionResult> AddTable(string tableNumber, int capacity)
        {
            var table = new Table
            {
                TableNumber = tableNumber,
                Capacity = capacity,
                Status = Core.Concretes.Enums.TableStatus.Empty
            };

            await _tableRepository.AddAsync(table);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = $"{tableNumber} başarıyla sisteme eklendi.", TableId = table.Id });
        }

        // ==========================================
        // 2. TÜM MASALARI LİSTELEME UCU (Mevcut Kodun)
        // ==========================================
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllTables()
        {
            var tables = await _tableRepository.GetAll().ToListAsync();
            return Ok(tables);
        }

        // ====================================================================
        // 3. SAHA HARİTASI (TABLE DASHBOARD) UCU - YENİ EKLENEN
        // ====================================================================
        // Garson tableti açtığında veya sayfayı yenilediğinde çalışan uç.
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _tableService.GetTableDashboardAsync();

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

            
    }
}