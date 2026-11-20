using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hekimler (giriş hesaplarıyla birlikte) ve haftalık müsaitlik aralıkları.
    // Ekleme, güncelleme ve silme başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface IVeterinarianService
    {
        Task<List<Veterinarian>> GetAllAsync();

        Task<Veterinarian?> GetByIdAsync(int id);

        Task<string?> CreateAsync(VeterinarianCreateViewModel model);

        Task<string?> UpdateAsync(VeterinarianFormViewModel model);

        Task<string?> DeleteAsync(int id);

        Task<string?> AddAvailabilityAsync(AvailabilityViewModel model);

        Task<string?> DeleteAvailabilityAsync(int veterinarianId, int availabilityId);
    }
}
