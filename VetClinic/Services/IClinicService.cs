using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Klinik bilgileri ve haftalık çalışma saatleri.
    // Güncelleme metotları başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface IClinicService
    {
        Task<Clinic?> GetClinicAsync();

        Task<string?> UpdateClinicAsync(ClinicViewModel model);

        Task<List<ClinicWorkingHour>> GetWorkingHoursAsync();

        Task<string?> UpdateWorkingHoursAsync(List<WorkingHourRowViewModel> days);
    }
}
