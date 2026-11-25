using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;

namespace VetClinic.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<ServiceReportDto>>> GetServiceReportAsync(DateOnly? from, DateOnly? to)
        {
            if (from != null && to != null && from > to)
            {
                return ServiceResult<List<ServiceReportDto>>.Validation("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            }

            var query = _context.Appointments.AsNoTracking();

            // Tarih aralığı randevunun başlangıcına göre uygulanır; bitiş günü de dahil olsun diye ertesi günün başından küçük alınır.
            if (from != null)
            {
                var fromTime = from.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.StartTime >= fromTime);
            }

            if (to != null)
            {
                var toExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(a => a.StartTime < toExclusive);
            }

            // GroupBy SQL'de GROUP BY'a çevrilir: her hizmet için tek satır. Ciro yalnızca tamamlanan randevulardan,
            // randevuda saklanan (randevu anındaki) ücretle hesaplanır; hizmetin bugünkü fiyatı raporu etkilemez.
            var rows = await query
                .GroupBy(a => new { a.ServiceId, a.Service.Name })
                .Select(g => new ServiceReportDto
                {
                    ServiceId = g.Key.ServiceId,
                    ServiceName = g.Key.Name,
                    AppointmentCount = g.Count(),
                    CompletedCount = g.Count(a => a.Status == AppointmentStatus.Completed),
                    Revenue = g.Sum(a => a.Status == AppointmentStatus.Completed ? a.Price : 0m)
                })
                .OrderByDescending(r => r.Revenue)
                .ThenBy(r => r.ServiceName)
                .ToListAsync();

            return ServiceResult<List<ServiceReportDto>>.Ok(rows);
        }
    }
}
