using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;

namespace VetClinic.Services
{
    public class AppointmentService : IAppointmentService
    {
        // Boş saatler bu adımla aranır: 09:00, 09:15, 09:30...
        private const int SlotStepMinutes = 15;

        // Zaman çakışmasında yalnızca bu durumdaki randevular saati doldurur; reddedilen ve iptal edilen randevular saati boşaltır.
        private static readonly AppointmentStatus[] BlockingStatuses = { AppointmentStatus.Pending, AppointmentStatus.Approved };

        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<VeterinarianSlotsDto>>> GetAvailableSlotsAsync(int serviceId, DateOnly date)
        {
            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceId);
            if (service == null)
            {
                return ServiceResult<List<VeterinarianSlotsDto>>.NotFound("Hizmet bulunamadı.");
            }

            if (!service.IsActive)
            {
                return ServiceResult<List<VeterinarianSlotsDto>>.Validation("Bu hizmet şu anda randevuya açık değil.");
            }

            var slots = new List<VeterinarianSlotsDto>();

            // O günün verileri (çalışma saati, hekimler, randevular) bir kez yüklenir; aday saatler bellekte denenir.
            // Her aday saat için veritabanına gidilseydi bir gün için yüzlerce sorgu çalışırdı.
            var workingHour = await _context.ClinicWorkingHours.AsNoTracking()
                .FirstOrDefaultAsync(h => h.DayOfWeek == date.DayOfWeek);
            if (workingHour == null || workingHour.IsClosed)
            {
                return ServiceResult<List<VeterinarianSlotsDto>>.Ok(slots); // Klinik kapalı: boş saat yok.
            }

            var veterinarians = await _context.Veterinarians
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.Availabilities)
                .Where(v => v.Services.Any(s => s.Id == serviceId))
                .OrderBy(v => v.User.FullName)
                .ToListAsync();

            var veterinarianIds = veterinarians.Select(v => v.Id).ToList();
            var dayAppointments = await GetVeterinarianAppointmentsOnDayAsync(veterinarianIds, date);

            var now = DateTime.Now;
            var opening = date.ToDateTime(workingHour.OpenTime);
            var closing = date.ToDateTime(workingHour.CloseTime);

            foreach (var veterinarian in veterinarians)
            {
                var veterinarianAppointments = dayAppointments.Where(a => a.VeterinarianId == veterinarian.Id).ToList();
                var startTimes = new List<TimeOnly>();

                // Açılıştan kapanışa kadar her 15 dakika bir aday başlangıçtır; kuralların hepsinden geçen aday boş saattir.
                for (var start = opening; start < closing; start = start.AddMinutes(SlotStepMinutes))
                {
                    var end = start.AddMinutes(service.DurationMinutes);
                    var check = CheckTimeRules(start, end, now, workingHour, veterinarian.Availabilities, veterinarianAppointments);
                    if (check.Success)
                    {
                        startTimes.Add(TimeOnly.FromDateTime(start));
                    }
                }

                if (startTimes.Count > 0)
                {
                    slots.Add(new VeterinarianSlotsDto
                    {
                        VeterinarianId = veterinarian.Id,
                        VeterinarianName = veterinarian.Title + " " + veterinarian.User.FullName,
                        StartTimes = startTimes
                    });
                }
            }

            return ServiceResult<List<VeterinarianSlotsDto>>.Ok(slots);
        }

        public async Task<ServiceResult<int>> CreateAsync(string ownerUserId, int petId, int serviceId, int veterinarianId, DateTime startTime)
        {
            // 1. Hayvan bu kullanıcıya ait mi? Başkasınınsa da "bulunamadı" denir; kaydın var olduğu bilgisi sızmaz.
            if (!await _context.Pets.AnyAsync(p => p.Id == petId && p.OwnerId == ownerUserId))
            {
                return ServiceResult<int>.NotFound("Hayvan bulunamadı.");
            }

            // 2. Hizmet ve hekim var mı (yoksa NotFound), hizmet aktif mi ve hekim bu hizmeti veriyor mu (Validation)?
            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceId);
            if (service == null)
            {
                return ServiceResult<int>.NotFound("Hizmet bulunamadı.");
            }

            var veterinarian = await _context.Veterinarians
                .AsNoTracking()
                .Include(v => v.Services)
                .Include(v => v.Availabilities)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.Id == veterinarianId);
            if (veterinarian == null)
            {
                return ServiceResult<int>.NotFound("Hekim bulunamadı.");
            }

            if (!service.IsActive)
            {
                return ServiceResult<int>.Validation("Bu hizmet şu anda randevuya açık değil.");
            }

            if (!veterinarian.Services.Any(s => s.Id == serviceId))
            {
                return ServiceResult<int>.Validation("Seçilen hekim bu hizmeti vermiyor.");
            }

            // 3-6. Ortak kontrol metodu. Bitiş saati formdan alınmaz, hizmetin süresinden hesaplanır.
            var endTime = startTime.AddMinutes(service.DurationMinutes);
            var date = DateOnly.FromDateTime(startTime);
            var workingHour = await _context.ClinicWorkingHours.AsNoTracking()
                .FirstOrDefaultAsync(h => h.DayOfWeek == date.DayOfWeek);
            var veterinarianAppointments = await GetVeterinarianAppointmentsOnDayAsync(new List<int> { veterinarianId }, date);

            var check = CheckTimeRules(startTime, endTime, DateTime.Now, workingHour, veterinarian.Availabilities, veterinarianAppointments);
            if (!check.Success)
            {
                return ServiceResult<int>.Fail(check.ErrorType, check.ErrorMessage!);
            }

            // 7. Aynı hayvanın aynı aralıkta başka bir randevusu var mı? (Başka bir hekimde olsa bile hayvan iki yerde olamaz.)
            var petAppointments = await GetPetAppointmentsOnDayAsync(petId, date);
            var petConflict = FindOverlapping(petAppointments, startTime, endTime);
            if (petConflict != null)
            {
                return ServiceResult<int>.Conflict(
                    $"Bu hayvanın {petConflict.StartTime:HH:mm}-{petConflict.EndTime:HH:mm} arasında başka bir randevusu var.");
            }

            // Süre ve ücret hizmetten kopyalanır: hizmet sonradan değişse de bu randevu ve ciro raporu değişmez.
            var appointment = new Appointment
            {
                PetId = petId,
                VeterinarianId = veterinarianId,
                ServiceId = serviceId,
                StartTime = startTime,
                EndTime = endTime,
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(appointment.Id);
        }

        // Ortak kontrol metodu: 3-6 numaralı kurallar yalnızca burada yazılır.
        // Veritabanına gitmez; gereken her şey parametre olarak gelir. GetAvailableSlotsAsync bunu her aday saat için,
        // CreateAsync tek bir aralık için çağırır. "now" da parametredir ki tüm adaylar aynı ana göre denensin.
        private static ServiceResult<bool> CheckTimeRules(DateTime start, DateTime end, DateTime now,
            ClinicWorkingHour? workingHour, ICollection<VeterinarianAvailability> veterinarianAvailabilities,
            List<Appointment> veterinarianAppointments)
        {
            var dayName = EnumTexts.ToTurkish(start.DayOfWeek);

            // 3. Başlangıç gelecekte mi?
            if (start <= now)
            {
                return ServiceResult<bool>.Validation("Randevunun başlangıcı ileri bir tarih ve saat olmalıdır.");
            }

            // 4. Aralığın tamamı kliniğin o günkü çalışma saatleri içinde mi?
            if (workingHour == null || workingHour.IsClosed)
            {
                return ServiceResult<bool>.Validation($"Klinik {dayName} günleri kapalıdır.");
            }

            var startOfDay = TimeOnly.FromDateTime(start);
            var endOfDay = TimeOnly.FromDateTime(end);

            // Gece yarısını geçen bir aralık (bitişin tarihi farklı) hiçbir çalışma saatine sığmaz.
            if (end.Date != start.Date || startOfDay < workingHour.OpenTime || endOfDay > workingHour.CloseTime)
            {
                return ServiceResult<bool>.Validation(
                    $"Randevu kliniğin {dayName} çalışma saatleri ({workingHour.OpenTime:HH:mm}-{workingHour.CloseTime:HH:mm}) içinde başlayıp bitmelidir. "
                    + $"Seçilen hizmet {(end - start).TotalMinutes} dakika sürer.");
            }

            // 5. Aralığın tamamı hekimin o günkü müsaitlik aralıklarından birinin içinde mi?
            var dayAvailabilities = veterinarianAvailabilities
                .Where(a => a.DayOfWeek == start.DayOfWeek)
                .OrderBy(a => a.StartTime)
                .ToList();

            if (!dayAvailabilities.Any(a => startOfDay >= a.StartTime && endOfDay <= a.EndTime))
            {
                if (dayAvailabilities.Count == 0)
                {
                    return ServiceResult<bool>.Validation($"Hekim {dayName} günleri çalışmıyor.");
                }

                var ranges = string.Join(", ", dayAvailabilities.Select(a => $"{a.StartTime:HH:mm}-{a.EndTime:HH:mm}"));
                return ServiceResult<bool>.Validation(
                    $"Hekim bu saatte müsait değil. Hekimin {dayName} müsaitlik saatleri: {ranges}. Randevunun tamamı bu aralıklardan birine sığmalıdır.");
            }

            // 6. Hekimin Bekliyor ya da Onaylandı durumundaki başka bir randevusuyla çakışıyor mu?
            var conflict = FindOverlapping(veterinarianAppointments, start, end);
            if (conflict != null)
            {
                return ServiceResult<bool>.Conflict(
                    $"Hekimin {conflict.StartTime:HH:mm}-{conflict.EndTime:HH:mm} arasında başka bir randevusu var.");
            }

            return ServiceResult<bool>.Ok(true);
        }

        // Çakışma koşulu tek yerde: yeniBaşlangıç < mevcutBitiş && mevcutBaşlangıç < yeniBitiş.
        // Uç uca gelen randevular (10:00'da biten ve 10:00'da başlayan) çakışmaz. Yalnızca BlockingStatuses'taki randevular sayılır.
        // Hem hekim (kural 6) hem hayvan (kural 7) kontrolünde kullanılır.
        private static Appointment? FindOverlapping(List<Appointment> appointments, DateTime start, DateTime end)
        {
            return appointments.FirstOrDefault(a => BlockingStatuses.Contains(a.Status)
                && start < a.EndTime
                && a.StartTime < end);
        }

        // Verilen hekimlerin o güne denk gelen randevuları (tüm durumlar; hangilerinin saati doldurduğuna FindOverlapping karar verir).
        private async Task<List<Appointment>> GetVeterinarianAppointmentsOnDayAsync(List<int> veterinarianIds, DateOnly date)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Where(a => veterinarianIds.Contains(a.VeterinarianId) && a.StartTime < dayEnd && a.EndTime > dayStart)
                .ToListAsync();
        }

        // Hayvanın o güne denk gelen randevuları (tüm durumlar).
        private async Task<List<Appointment>> GetPetAppointmentsOnDayAsync(int petId, DateOnly date)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.PetId == petId && a.StartTime < dayEnd && a.EndTime > dayStart)
                .ToListAsync();
        }
    }
}
