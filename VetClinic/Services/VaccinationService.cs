using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class VaccinationService : IVaccinationService
    {
        private readonly ApplicationDbContext _context;

        public VaccinationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VaccinationRecord?> GetMyRecordAsync(string ownerId, int recordId)
        {
            // Aşı kaydında sahip bilgisi yok; hayvan üzerinden (r.Pet.OwnerId) filtrelenir. EF bunu SQL'de JOIN'e çevirir.
            return await _context.VaccinationRecords
                .AsNoTracking()
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r => r.Id == recordId && r.Pet.OwnerId == ownerId);
        }

        public async Task<string?> CreateAsync(string ownerId, VaccinationFormViewModel model)
        {
            // PetId formdan geldiği için hayvanın bu kullanıcıya ait olduğu ayrıca kontrol edilir.
            if (!await _context.Pets.AnyAsync(p => p.Id == model.PetId && p.OwnerId == ownerId))
            {
                return "Hayvan bulunamadı.";
            }

            var error = ValidateDates(model);
            if (error != null)
            {
                return error;
            }

            _context.VaccinationRecords.Add(new VaccinationRecord
            {
                PetId = model.PetId,
                VaccineName = model.VaccineName.Trim(),
                AdministeredOn = model.AdministeredOn,
                NextDoseOn = model.NextDoseOn
            });
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> UpdateAsync(string ownerId, VaccinationFormViewModel model)
        {
            var record = await _context.VaccinationRecords
                .FirstOrDefaultAsync(r => r.Id == model.Id && r.Pet.OwnerId == ownerId);
            if (record == null)
            {
                return "Aşı kaydı bulunamadı.";
            }

            var error = ValidateDates(model);
            if (error != null)
            {
                return error;
            }

            // Kaydın hangi hayvana ait olduğu değiştirilmez; formdan gelen PetId burada kullanılmaz.
            record.VaccineName = model.VaccineName.Trim();
            record.AdministeredOn = model.AdministeredOn;
            record.NextDoseOn = model.NextDoseOn;

            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> DeleteAsync(string ownerId, int recordId)
        {
            var record = await _context.VaccinationRecords
                .FirstOrDefaultAsync(r => r.Id == recordId && r.Pet.OwnerId == ownerId);
            if (record == null)
            {
                return "Aşı kaydı bulunamadı.";
            }

            _context.VaccinationRecords.Remove(record);
            await _context.SaveChangesAsync();
            return null;
        }

        // Ekleme ve güncellemede ortak tarih kuralları.
        private static string? ValidateDates(VaccinationFormViewModel model)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.AdministeredOn > today)
            {
                return "Aşının yapıldığı tarih bugünden ileri olamaz.";
            }

            if (model.NextDoseOn != null && model.NextDoseOn <= model.AdministeredOn)
            {
                return "Sonraki doz tarihi, aşının yapıldığı tarihten sonra olmalıdır.";
            }

            return null;
        }
    }
}
