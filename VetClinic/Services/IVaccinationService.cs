using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hayvanların aşı kayıtları. Kayıt, hayvanı üzerinden sahibine bağlıdır; her metot sahibin id'sini alır.
    // Ekleme yeni kaydın id'sini, güncelleme ve silme bool döner; hepsi ServiceResult içinde.
    public interface IVaccinationService
    {
        Task<VaccinationRecord?> GetMyRecordAsync(string ownerId, int recordId);

        Task<ServiceResult<int>> CreateAsync(string ownerId, VaccinationFormViewModel model);

        Task<ServiceResult<bool>> UpdateAsync(string ownerId, VaccinationFormViewModel model);

        Task<ServiceResult<bool>> DeleteAsync(string ownerId, int recordId);
    }
}
