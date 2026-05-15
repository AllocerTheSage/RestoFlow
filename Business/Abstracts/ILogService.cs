using Business.DTOs.LogDtos;
using Core.Concretes.Enums;
using Core.Abstracts;

namespace Business.Abstracts
{
    public interface ILogService
    {
        // 1. İÇERİDEN BESLEME (YAZMA): Sistemdeki diğer Manager'lar bu metodu çağırıp kayıt tutturacak.
        Task AddLogAsync(LogType logType, string? userId, string message, string? details = null, int? relatedEntityId = null);

        // 2. DIŞARIYA VERİ VERME (OKUMA): Frontend (log.js) bizden verileri istediğinde bu metot çalışacak.
        Task<IDataResult<List<SystemLogDto>>> GetAllLogsAsync();
    }
}