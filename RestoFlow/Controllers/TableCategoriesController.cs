using Business.Abstracts;
using Business.DTOs.TableCategoryDtos;
using Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestoFlow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Tüm kategori işlemleri varsayılan olarak Admin yetkisi gerektirir.
    [Authorize(Roles = "Admin")]
    public class TableCategoriesController : ControllerBase
    {
        private readonly ITableCategoryService _categoryService;

        public TableCategoriesController(ITableCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/TableCategories/getall
        [HttpGet("getall")]
        // Frontend'de masaları listelerken Garsonlar da kategorileri görmeli, o yüzden bu metoda özel izni gevşetiyoruz (Herkes erişebilir).
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // POST: api/TableCategories/create
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] TableCategoryCreateDto createDto)
        {
            var result = await _categoryService.CreateCategoryAsync(createDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // PUT: api/TableCategories/update
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] TableCategoryUpdateDto updateDto)
        {
            var result = await _categoryService.UpdateCategoryAsync(updateDto);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }

        // DELETE: api/TableCategories/delete/5
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
    }
}