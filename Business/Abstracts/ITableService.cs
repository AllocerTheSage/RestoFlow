using Business.DTOs.TableDtos;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface ITableService
    {
        // Tüm masaları, o anki durumları ve (eğer varsa) açık hesap tutarlarıyla birlikte getirir.
        Task<IDataResult<List<TableDto>>> GetTableDashboardAsync();
    }
}