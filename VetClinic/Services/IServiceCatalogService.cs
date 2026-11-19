using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Klinikte verilen hizmetler (muayene, aşı vb.). Entity'nin adı Service olduğu için "ServiceService" yerine
    // "hizmet kataloğu" anlamında ServiceCatalogService adı verildi.
    // Ekleme, güncelleme ve silme başarılıysa null, değilse kullanıcıya gösterilecek hata mesajını döner.
    public interface IServiceCatalogService
    {
        Task<List<Service>> GetAllAsync();

        Task<Service?> GetByIdAsync(int id);

        Task<string?> CreateAsync(ServiceFormViewModel model);

        Task<string?> UpdateAsync(ServiceFormViewModel model);

        Task<string?> DeleteAsync(int id);
    }
}
