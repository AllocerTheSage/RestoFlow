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
        private readonly ILogger<TableManager> _logger;

        public TableManager(IGenericRepository<Table> tableRepository, ILogger<TableManager> logger)
        {
            _tableRepository = tableRepository;
            _logger = logger;
        }

        public async Task<IDataResult<List<TableDto>>> GetTableDashboardAsync()
        {
            // 1. ADIM: Masaları ve her masanın SADECE açık olan adisyonunu çekiyoruz.
            // (Geçmişte kapanmış veya iptal edilmiş adisyonları bu haritaya dahil etmiyoruz).
            var tables = await _tableRepository.GetAll()
                .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Canceled))
                .ToListAsync();

            // 2. ADIM: Veritabanı modelini, dış dünyaya çıkacak DTO'ya dönüştürüyoruz (Manuel Mapping).
            var tableDtos = tables.Select(t => new TableDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status,

                // Eğer masanın içinde o an açık bir sipariş (Order) varsa TotalPrice'ını al, yoksa hesabı 0.00 TL göster.
                // ESKİ HALİ:
                // ActiveOrderTotal = t.Orders.FirstOrDefault()?.TotalPrice ?? 0m

                // YENİ HALİ (Akıllı Hesaplama):
                ActiveOrderTotal = t.Orders.Select(o => o.TotalPrice - o.PaidAmount).FirstOrDefault()
            }).ToList();

            _logger.LogInformation("Masa haritası (Dashboard) görüntülendi. Toplam Masa: {Count}", tableDtos.Count);

            return new SuccessDataResult<List<TableDto>>(tableDtos, "Saha haritası başarıyla getirildi.");
        }
    }
}