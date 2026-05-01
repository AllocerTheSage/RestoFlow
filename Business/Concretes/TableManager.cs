using Business.Abstracts;
using Business.DTOs.TableDtos;
using Core.Abstracts;
using Core.Abstracts.IRepositories;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Core.Concretes.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Concretes
{
    public class TableManager : ITableService
    {
        private readonly IGenericRepository<Table> _tableRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TableManager> _logger;

        public TableManager(IGenericRepository<Table> tableRepository, IUnitOfWork unitOfWork, ILogger<TableManager> logger)
        {
            _tableRepository = tableRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IDataResult<List<TableDto>>> GetTableDashboardAsync()
        {
            var tables = await _tableRepository.GetAll()
                .Include(t => t.Category) // ÇOK ÖNEMLİ: Kategori adını (Örn: Bahçe) çekebilmek için
                .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled))
                .ToListAsync();

            var tableDtos = tables.Select(t => new TableDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status,
                ActiveOrderTotal = t.Orders.Select(o => o.TotalPrice - o.PaidAmount).FirstOrDefault(),

                // YENİ: İlişkili tablodan kategorinin adını alıyoruz
                CategoryName = t.Category != null ? t.Category.Name : "Genel"
            }).ToList();

            _logger.LogInformation("Masa haritası görüntülendi. Toplam Masa: {Count}", tableDtos.Count);

            return new SuccessDataResult<List<TableDto>>(tableDtos, "Saha haritası başarıyla getirildi.");
        }

        public async Task<IResult> CreateTableAsync(TableCreateDto createDto)
        {
            var isTableExists = await _tableRepository.Where(t => t.TableNumber.ToLower() == createDto.TableNumber.ToLower()).AnyAsync();
            if (isTableExists)
            {
                return new ErrorResult($"'{createDto.TableNumber}' adında bir masa zaten mevcut!");
            }

            var newTable = new Table
            {
                TableNumber = createDto.TableNumber,
                Capacity = createDto.Capacity,
                Status = TableStatus.Empty,

                // YENİ: Frontend'den gelen Kategori ID'sini (Örn: 1) veritabanına işliyoruz
                CategoryId = createDto.CategoryId
            };

            await _tableRepository.AddAsync(newTable);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Yeni masa başarıyla sisteme eklendi.");
        }

        public async Task<IResult> UpdateTableAsync(TableUpdateDto updateDto)
        {
            var table = await _tableRepository.GetByIdAsync(updateDto.Id);
            if (table == null) return new ErrorResult("Masa bulunamadı.");

            table.TableNumber = updateDto.TableNumber;
            table.Capacity = updateDto.Capacity;

            // YENİ: Masanın kategorisini güncelliyoruz
            table.CategoryId = updateDto.CategoryId;

            _tableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Masa bilgileri başarıyla güncellendi.");
        }

        public async Task<IResult> DeleteTableAsync(int id)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null) return new ErrorResult("Masa bulunamadı.");

            if (table.Status != TableStatus.Empty)
            {
                return new ErrorResult("Dolu veya rezerve olan bir masa silinemez. Lütfen önce hesabı kapatın.");
            }

            _tableRepository.Delete(table);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Masa sistemden tamamen silindi.");
        }
        // YENİ: Masayı tek tıkla Rezerve Et / İptal Et
        public async Task<IResult> ToggleReservationAsync(int tableId)
        {
            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null) return new ErrorResult("Masa bulunamadı.");

            // Masa doluysa müdahale ettirmiyoruz
            if (table.Status == TableStatus.Occupied)
            {
                return new ErrorResult("Dolu bir masa rezerve edilemez veya rezervasyonu iptal edilemez.");
            }

            // Masa boşsa -> Rezerve yap
            if (table.Status == TableStatus.Empty)
            {
                table.Status = TableStatus.Reserved;
            }
            // Masa rezerveyse -> Boş yap (İptal et)
            else if (table.Status == TableStatus.Reserved)
            {
                table.Status = TableStatus.Empty;
            }

            _tableRepository.Update(table);
            await _unitOfWork.SaveChangesAsync();

            return new SuccessResult("Masa rezervasyon durumu başarıyla güncellendi.");
        }
    }
}
