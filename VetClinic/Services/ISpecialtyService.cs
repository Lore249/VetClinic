using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hekim uzmanlık alanları (Dermatoloji, Dahiliye vb.).
    // Ekleme yeni kaydın id'sini, güncelleme ve silme bool döner; hepsi ServiceResult içinde.
    public interface ISpecialtyService
    {
        Task<List<Specialty>> GetAllAsync();

        Task<Specialty?> GetByIdAsync(int id);

        Task<ServiceResult<int>> CreateAsync(SpecialtyFormViewModel model);

        Task<ServiceResult<bool>> UpdateAsync(SpecialtyFormViewModel model);

        Task<ServiceResult<bool>> DeleteAsync(int id);
    }
}
