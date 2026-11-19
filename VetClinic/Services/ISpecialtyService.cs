using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Hekim uzmanlık alanları (Dermatoloji, Dahiliye vb.).
    // Ekleme, güncelleme ve silme başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface ISpecialtyService
    {
        Task<List<Specialty>> GetAllAsync();

        Task<Specialty?> GetByIdAsync(int id);

        Task<string?> CreateAsync(SpecialtyFormViewModel model);

        Task<string?> UpdateAsync(SpecialtyFormViewModel model);

        Task<string?> DeleteAsync(int id);
    }
}
