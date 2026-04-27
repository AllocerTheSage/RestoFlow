using Business.DTOs.TableCategoryDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface ITableCategoryService
    {
        Task<IDataResult<List<TableCategoryDto>>> GetAllCategoriesAsync();
        Task<IResult> CreateCategoryAsync(TableCategoryCreateDto createDto);
        Task<IResult> UpdateCategoryAsync(TableCategoryUpdateDto updateDto);
        Task<IResult> DeleteCategoryAsync(int id);
    }
}