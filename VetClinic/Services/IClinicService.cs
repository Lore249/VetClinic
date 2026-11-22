using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Klinik bilgileri ve haftalık çalışma saatleri. Güncelleme metotları sonucu ServiceResult olarak döner.
    public interface IClinicService
    {
        Task<Clinic?> GetClinicAsync();

        Task<ServiceResult<bool>> UpdateClinicAsync(ClinicViewModel model);

        Task<List<ClinicWorkingHour>> GetWorkingHoursAsync();

        Task<ServiceResult<bool>> UpdateWorkingHoursAsync(List<WorkingHourRowViewModel> days);
    }
}
