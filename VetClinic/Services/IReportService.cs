using VetClinic.Models.Dtos;

namespace VetClinic.Services
{
    // Admin raporları.
    public interface IReportService
    {
        // Hizmet türüne göre randevu sayısı ve tamamlanan randevuların cirosu. Tarihler isteğe bağlıdır ve dahildir.
        Task<ServiceResult<List<ServiceReportDto>>> GetServiceReportAsync(DateOnly? from, DateOnly? to);
    }
}
