using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hayvanların aşı kayıtları. Kayıt, hayvanı üzerinden sahibine bağlıdır; her metot sahibin id'sini alır.
    // Ekleme, güncelleme ve silme başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface IVaccinationService
    {
        Task<VaccinationRecord?> GetMyRecordAsync(string ownerId, int recordId);

        Task<string?> CreateAsync(string ownerId, VaccinationFormViewModel model);

        Task<string?> UpdateAsync(string ownerId, VaccinationFormViewModel model);

        Task<string?> DeleteAsync(string ownerId, int recordId);
    }
}
