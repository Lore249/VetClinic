using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Üyenin evcil hayvanları. Her metot sahibin id'sini alır ve yalnızca o kullanıcının kayıtlarıyla çalışır.
    // Ekleme, güncelleme ve silme başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface IPetService
    {
        Task<List<Pet>> GetMyPetsAsync(string ownerId);

        Task<Pet?> GetMyPetAsync(string ownerId, int petId);

        Task<string?> CreateAsync(string ownerId, PetFormViewModel model);

        Task<string?> UpdateAsync(string ownerId, PetFormViewModel model);

        Task<string?> DeleteAsync(string ownerId, int petId);
    }
}
