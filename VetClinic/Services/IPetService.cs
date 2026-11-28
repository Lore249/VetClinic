using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Üyenin evcil hayvanları. Her metot sahibin id'sini alır ve yalnızca o kullanıcının kayıtlarıyla çalışır.
    // Ekleme yeni kaydın id'sini, güncelleme ve silme bool döner; hepsi ServiceResult içinde.
    public interface IPetService
    {
        Task<List<Pet>> GetMyPetsAsync(string ownerId);

        Task<Pet?> GetMyPetAsync(string ownerId, int petId);

        // MCP araçları hayvanı adıyla bulur. Ad Türkçe kurallarla, büyük/küçük harfe duyarsız karşılaştırılır (FISTIK = fıstık).
        // Kullanıcının bu adda hayvanı yoksa null döner.
        Task<Pet?> FindMyPetByNameAsync(string ownerId, string name);

        Task<ServiceResult<int>> CreateAsync(string ownerId, PetFormViewModel model);

        Task<ServiceResult<bool>> UpdateAsync(string ownerId, PetFormViewModel model);

        Task<ServiceResult<bool>> DeleteAsync(string ownerId, int petId);
    }
}
