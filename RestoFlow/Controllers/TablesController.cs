using Business.Abstracts;
using Business.DTOs.TableDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestoFlow.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bu controller'a sadece sisteme giriş yapmış personeller erişebilir
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;

        // Bütün işi akıllı yöneticimiz (TableManager) yapacağı için sadece onu içeri alıyoruz.
        public TablesController(ITableService tableService)
        {
            _tableService = tableService;
        }

        // ==========================================
        // 1. YENİ MASA EKLEME UCU (Mevcut Kodun)
        // ==========================================
        [HttpPost("add")]
        [Authorize(Policy = Permissions.Administration.ManageLayout)] // SADECE PATRON/YÖNETİCİ MASA EKLEYEBİLİR!
        public async Task<IActionResult> AddTable(string tableNumber, int capacity)
        {
            var result = await _tableService.GetTableDashboardAsync();
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // ====================================================================
        // 2. YENİ MASA EKLEME
        // ====================================================================
        // POST: api/Tables/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin")] // Sadece Admin olanlar masa ekleyebilir
        public async Task<IActionResult> CreateTable([FromBody] TableCreateDto createDto)
        {
            var result = await _tableService.CreateTableAsync(createDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // ====================================================================
        // 3. MASA BİLGİLERİNİ GÜNCELLEME
        // ====================================================================
        // PUT: api/Tables/update
        [HttpPut("update")]
        [Authorize(Roles = "Admin")] // Sadece Admin olanlar masayı düzenleyebilir
        public async Task<IActionResult> UpdateTable([FromBody] TableUpdateDto updateDto)
        {
            var result = await _tableService.UpdateTableAsync(updateDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // ====================================================================
        // 4. MASA SİLME
        // ====================================================================
        // DELETE: api/Tables/delete/5
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")] // Sadece Admin olanlar masayı silebilir
        public async Task<IActionResult> DeleteTable(int id)
        {
            var result = await _tableService.DeleteTableAsync(id);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

            
    }
}