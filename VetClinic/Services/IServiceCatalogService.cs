using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    // Klinikte verilen hizmetler (muayene, aşı vb.). Entity'nin adı Service olduğu için "ServiceService" yerine
    // "hizmet kataloğu" anlamında ServiceCatalogService adı verildi.
    // Ekleme yeni kaydın id'sini, güncelleme ve silme bool döner; hepsi ServiceResult içinde.
    public interface IServiceCatalogService
    {
        Task<List<Service>> GetAllAsync();

        // Yalnızca aktif hizmetler: randevu formunda (ve sonraki günlerde ana sayfa ile MCP'de) bunlar gösterilir.
        Task<List<Service>> GetActiveAsync();

        Task<Service?> GetByIdAsync(int id);

        Task<ServiceResult<int>> CreateAsync(ServiceFormViewModel model);

        Task<ServiceResult<bool>> UpdateAsync(ServiceFormViewModel model);

        Task<ServiceResult<bool>> DeleteAsync(int id);
    }
}
