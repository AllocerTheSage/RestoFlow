using Business.DTOs.TableDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface ITableService
    {
        Task<IDataResult<List<TableDto>>> GetTableDashboardAsync();

        // EKLENEN YENİ METOTLAR
        Task<IResult> UpdateTableAsync(TableUpdateDto updateDto);
        Task<IResult> DeleteTableAsync(int id);
        Task<IResult> CreateTableAsync(TableCreateDto createDto);
    }
}