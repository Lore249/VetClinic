using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class ServiceCatalogService : IServiceCatalogService
    {
        private readonly ApplicationDbContext _context;

        public ServiceCatalogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetAllAsync()
        {
            // Admin pasif hizmetleri de görür; aktifler önce listelenir.
            return await _context.Services
                .AsNoTracking()
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Service?> GetByIdAsync(int id)
        {
            return await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<ServiceResult<int>> CreateAsync(ServiceFormViewModel model)
        {
            var name = model.Name.Trim();

            if (await NameExistsAsync(name, 0))
            {
                return ServiceResult<int>.Validation("Bu adla bir hizmet zaten var.");
            }

            var service = new Service
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                DurationMinutes = model.DurationMinutes,
                Price = model.Price,
                IsActive = model.IsActive
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(service.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(ServiceFormViewModel model)
        {
            var service = await _context.Services.FindAsync(model.Id);
            if (service == null)
            {
                return ServiceResult<bool>.NotFound("Hizmet bulunamadı.");
            }

            var name = model.Name.Trim();

            if (await NameExistsAsync(name, model.Id))
            {
                return ServiceResult<bool>.Validation("Bu adla bir hizmet zaten var.");
            }

            // Süre ve ücret değişse de eski randevular etkilenmez: randevu, alındığı andaki değerlerin kopyasını saklar.
            service.Name = name;
            service.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            service.DurationMinutes = model.DurationMinutes;
            service.Price = model.Price;
            service.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return ServiceResult<bool>.NotFound("Hizmet bulunamadı.");
            }

            // İlişki Restrict olduğu için randevusu olan hizmet silinmeye çalışılırsa veritabanı hata verir.
            // Bu yüzden önce kontrol edilir ve kullanıcıya ne yapabileceği söylenir.
            if (await _context.Appointments.AnyAsync(a => a.ServiceId == id))
            {
                return ServiceResult<bool>.Validation("Bu hizmete ait randevular olduğu için silinemez. Yeni randevularda görünmemesi için hizmeti düzenleyip pasif yapabilirsiniz.");
            }

            // Hekimlerle olan bağlar (VeterinarianServices satırları) veritabanında Cascade ile silinir.
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // Aynı adlı iki hizmet, randevu formunda hangisinin seçildiğini belirsizleştirir.
        // Düzenlemede kaydın kendisi sayılmasın diye kendi Id'si hariç tutulur.
        private async Task<bool> NameExistsAsync(string name, int excludedId)
        {
            return await _context.Services.AnyAsync(s => s.Name == name && s.Id != excludedId);
        }
    }
}
