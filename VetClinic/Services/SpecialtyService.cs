using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly ApplicationDbContext _context;

        public SpecialtyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Specialty>> GetAllAsync()
        {
            // Listede her uzmanlığın kaç hekimde tanımlı olduğu gösterildiği için hekimler de yüklenir.
            return await _context.Specialties
                .AsNoTracking()
                .Include(s => s.Veterinarians)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Specialty?> GetByIdAsync(int id)
        {
            // Silme onayında uzmanlığın hangi hekimlerden kaldırılacağı adlarıyla gösterilir.
            // Hekimin adı ApplicationUser'da durduğu için ThenInclude ile kullanıcı da yüklenir.
            return await _context.Specialties
                .AsNoTracking()
                .Include(s => s.Veterinarians)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<ServiceResult<int>> CreateAsync(SpecialtyFormViewModel model)
        {
            var name = model.Name.Trim();

            if (await NameExistsAsync(name, 0))
            {
                return ServiceResult<int>.Validation("Bu adla bir uzmanlık zaten var.");
            }

            var specialty = new Specialty { Name = name };
            _context.Specialties.Add(specialty);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(specialty.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(SpecialtyFormViewModel model)
        {
            var specialty = await _context.Specialties.FindAsync(model.Id);
            if (specialty == null)
            {
                return ServiceResult<bool>.NotFound("Uzmanlık bulunamadı.");
            }

            var name = model.Name.Trim();

            if (await NameExistsAsync(name, model.Id))
            {
                return ServiceResult<bool>.Validation("Bu adla bir uzmanlık zaten var.");
            }

            specialty.Name = name;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null)
            {
                return ServiceResult<bool>.NotFound("Uzmanlık bulunamadı.");
            }

            // Hekimlerle olan bağlar (VeterinarianSpecialties satırları) veritabanında Cascade ile silinir; hekimlere dokunulmaz.
            _context.Specialties.Remove(specialty);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // Aynı adlı iki uzmanlık, hekim formunda hangisinin seçildiğini belirsizleştirir.
        // Düzenlemede kaydın kendisi sayılmasın diye kendi Id'si hariç tutulur.
        private async Task<bool> NameExistsAsync(string name, int excludedId)
        {
            return await _context.Specialties.AnyAsync(s => s.Name == name && s.Id != excludedId);
        }
    }
}
