using Business.Abstracts;
using Business.DTOs.LogDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;

namespace Business.Concretes
{
    public class LogManager : ILogService
    {
        // Veritabanı araçlarımızı (Repository ve Kaydetme Uzmanı UnitOfWork) içeri alıyoruz.
        private readonly IGenericRepository<SystemLog> _logRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LogManager(IGenericRepository<SystemLog> logRepository, IUnitOfWork unitOfWork)
        {
            _logRepository = logRepository;
            _unitOfWork = unitOfWork;
        }

        // 1. YAZMA İŞLEMİ (Veritabanına yeni bir satır ekler)
        public async Task AddLogAsync(LogType logType, string? userId, string message, string? details = null, int? relatedEntityId = null)
        {
            var log = new SystemLog
            {
                LogType = logType,
                UserId = userId,
                Message = message,
                Details = details,
                RelatedEntityId = relatedEntityId
                // CreatedDate özelliğine biz dokunmuyoruz, BaseEntity otomatik olarak o anın saatini basıyor!
            };

            await _logRepository.AddAsync(log);
            await _unitOfWork.SaveChangesAsync(); // SQL'e "Kaydet" emrini veriyoruz.
        }

        // 2. OKUMA İŞLEMİ (Frontend patron ekranı için verileri çeker)
        public async Task<IDataResult<List<SystemLogDto>>> GetAllLogsAsync()
        {
            // Tüm logları veritabanından çekerken, AppUser tablosuyla birleştirir (Include).
            // En yeni olay en üstte gelsin diye tarihe göre ters sıralar (OrderByDescending).
            // Ve son olarak sonsuz döngüden kaçmak için SystemLogDto isimli kuryemize (Select) dönüştürür.
            var logs = await _logRepository.GetAll()
                .Include(x => x.User)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new SystemLogDto
                {
                    Id = x.Id,
                    LogType = x.LogType.ToString(),
                    // Eğer UserId boşsa "Sistem" yaz, doluysa adamın Adı Soyadını birleştirip yaz.
                    UserName = x.User != null ? x.User.FirstName + " " + x.User.LastName : "Sistem",
                    Message = x.Message,
                    Details = x.Details,
                    RelatedEntityId = x.RelatedEntityId,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();

            return new SuccessDataResult<List<SystemLogDto>>(logs, "Loglar başarıyla getirildi.");
        }
    }
}