using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Helpers;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class ClinicService : IClinicService
    {
        private readonly ApplicationDbContext _context;

        public ClinicService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Clinic?> GetClinicAsync()
        {
            // Tek klinik olduğu için tablodaki ilk (ve tek) satır yeterli.
            return await _context.Clinics.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<bool>> UpdateClinicAsync(ClinicViewModel model)
        {
            var clinic = await _context.Clinics.FirstOrDefaultAsync();
            if (clinic == null)
            {
                return ServiceResult<bool>.NotFound("Klinik kaydı bulunamadı.");
            }

            clinic.Name = model.Name.Trim();
            clinic.Address = model.Address.Trim();
            clinic.Phone = model.Phone.Trim();

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<List<ClinicWorkingHour>> GetWorkingHoursAsync()
        {
            var hours = await _context.ClinicWorkingHours.AsNoTracking().ToListAsync();

            // DayOfWeek'te Pazar 0 olduğu için düz sıralamada en başa gelir; liste Pazartesi'den başlasın diye Pazar sona alınır.
            return hours
                .OrderBy(h => h.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)h.DayOfWeek)
                .ToList();
        }

        public async Task<ServiceResult<bool>> UpdateWorkingHoursAsync(List<WorkingHourRowViewModel> days)
        {
            // Önce tüm günler kontrol edilir; bir gün hatalıysa hiçbir gün kaydedilmez.
            foreach (var day in days)
            {
                if (!day.IsClosed && day.OpenTime >= day.CloseTime)
                {
                    return ServiceResult<bool>.Validation(EnumTexts.ToTurkish(day.DayOfWeek) + ": açılış saati kapanış saatinden önce olmalıdır.");
                }
            }

            var hours = await _context.ClinicWorkingHours.ToListAsync();
            foreach (var hour in hours)
            {
                var day = days.FirstOrDefault(d => d.DayOfWeek == hour.DayOfWeek);
                if (day == null)
                {
                    continue; // Formdan gelmeyen gün olduğu gibi kalır.
                }

                hour.IsClosed = day.IsClosed;
                hour.OpenTime = day.OpenTime;
                hour.CloseTime = day.CloseTime;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}
