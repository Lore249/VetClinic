using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hekimler (giriş hesaplarıyla birlikte) ve haftalık müsaitlik aralıkları.
    // Ekleme metotları yeni kaydın id'sini, güncelleme ve silme bool döner; hepsi ServiceResult içinde.
    public interface IVeterinarianService
    {
        Task<List<Veterinarian>> GetAllAsync();

        Task<Veterinarian?> GetByIdAsync(int id);

        Task<ServiceResult<int>> CreateAsync(VeterinarianCreateViewModel model);

        Task<ServiceResult<bool>> UpdateAsync(VeterinarianFormViewModel model);

        Task<ServiceResult<bool>> DeleteAsync(int id);

        Task<ServiceResult<int>> AddAvailabilityAsync(AvailabilityViewModel model);

        Task<ServiceResult<bool>> DeleteAvailabilityAsync(int veterinarianId, int availabilityId);
    }
}
