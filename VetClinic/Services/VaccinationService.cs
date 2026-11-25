using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Dtos;
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

        public async Task<ServiceResult<VaccinationScheduleDto>> GetScheduleAsync(string ownerId, int petId)
        {
            // Sahiplik filtresi: başkasının hayvanı "bulunamadı"dır.
            var pet = await _context.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == petId && p.OwnerId == ownerId);
            if (pet == null)
            {
                return ServiceResult<VaccinationScheduleDto>.NotFound("Hayvan bulunamadı.");
            }

            var records = await _context.VaccinationRecords
                .AsNoTracking()
                .Where(r => r.PetId == petId)
                .Select(r => new VaccinationDto
                {
                    Id = r.Id,
                    VaccineName = r.VaccineName,
                    AdministeredOn = r.AdministeredOn,
                    NextDoseOn = r.NextDoseOn
                })
                .ToListAsync();

            // Hangi kaydın yaklaşan, hangisinin gecikmiş sayılacağı yalnızca burada belirlenir.
            var today = DateOnly.FromDateTime(DateTime.Today);

            var schedule = new VaccinationScheduleDto
            {
                PetId = pet.Id,
                PetName = pet.Name,
                Past = records.OrderByDescending(r => r.AdministeredOn).ToList(),
                Upcoming = records.Where(r => r.NextDoseOn >= today).OrderBy(r => r.NextDoseOn).ToList(),
                Overdue = records.Where(r => r.NextDoseOn < today).OrderBy(r => r.NextDoseOn).ToList()
            };

            return ServiceResult<VaccinationScheduleDto>.Ok(schedule);
        }

        public async Task<ServiceResult<int>> CreateAsync(string ownerId, VaccinationFormViewModel model)
        {
            // PetId formdan geldiği için hayvanın bu kullanıcıya ait olduğu ayrıca kontrol edilir.
            if (!await _context.Pets.AnyAsync(p => p.Id == model.PetId && p.OwnerId == ownerId))
            {
                return ServiceResult<int>.NotFound("Hayvan bulunamadı.");
            }

            var error = ValidateDates(model);
            if (error != null)
            {
                return ServiceResult<int>.Validation(error);
            }

            var record = new VaccinationRecord
            {
                PetId = model.PetId,
                VaccineName = model.VaccineName.Trim(),
                AdministeredOn = model.AdministeredOn,
                NextDoseOn = model.NextDoseOn
            };
            _context.VaccinationRecords.Add(record);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(record.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(string ownerId, VaccinationFormViewModel model)
        {
            var record = await _context.VaccinationRecords
                .FirstOrDefaultAsync(r => r.Id == model.Id && r.Pet.OwnerId == ownerId);
            if (record == null)
            {
                return ServiceResult<bool>.NotFound("Aşı kaydı bulunamadı.");
            }

            var error = ValidateDates(model);
            if (error != null)
            {
                return ServiceResult<bool>.Validation(error);
            }

            // Kaydın hangi hayvana ait olduğu değiştirilmez; formdan gelen PetId burada kullanılmaz.
            record.VaccineName = model.VaccineName.Trim();
            record.AdministeredOn = model.AdministeredOn;
            record.NextDoseOn = model.NextDoseOn;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(string ownerId, int recordId)
        {
            var record = await _context.VaccinationRecords
                .FirstOrDefaultAsync(r => r.Id == recordId && r.Pet.OwnerId == ownerId);
            if (record == null)
            {
                return ServiceResult<bool>.NotFound("Aşı kaydı bulunamadı.");
            }

            _context.VaccinationRecords.Remove(record);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // Ekleme ve güncellemede ortak tarih kuralları (Validation türünde). Uygunsa null, değilse hata mesajı döner.
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
