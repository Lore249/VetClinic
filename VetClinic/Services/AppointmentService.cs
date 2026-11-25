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

            // 2. Hizmet var mı? Süre ve ücret hizmetten kopyalanacağı için burada yüklenir.
            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceId);
            if (service == null)
            {
                return ServiceResult<int>.NotFound("Hizmet bulunamadı.");
            }

            // 2-7. CreateAsync ve UpdateAsync'in ortak kontrolleri. Yeni randevuda hariç tutulacak randevu yoktur (0).
            var check = await CheckBookingRulesAsync(service, petId, veterinarianId, startTime, service.DurationMinutes, 0);
            if (!check.Success)
            {
                return ServiceResult<int>.Fail(check.ErrorType, check.ErrorMessage!);
            }

            // Süre ve ücret hizmetten kopyalanır: hizmet sonradan değişse de bu randevu ve ciro raporu değişmez.
            var appointment = new Appointment
            {
                PetId = petId,
                VeterinarianId = veterinarianId,
                ServiceId = serviceId,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(service.DurationMinutes),
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(appointment.Id);
        }

        public async Task<List<Appointment>> GetAppointmentsAsync(string userId, string userRole)
        {
            // Hangi randevuların görüneceğine VisibleTo karar verir; burada yalnızca ilişkili veriler eklenir ve sıralanır.
            return await VisibleTo(userId, userRole)
                .AsNoTracking()
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Owner)
                .Include(a => a.Service)
                .Include(a => a.Veterinarian)
                    .ThenInclude(v => v.User)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentAsync(int appointmentId, string userId, string userRole)
        {
            // Kullanıcının göremediği randevunun id'si yazılırsa null döner (controller 404 gösterir); kaydın var olduğu bilgisi sızmaz.
            return await VisibleTo(userId, userRole)
                .AsNoTracking()
                .Include(a => a.Pet)
                    .ThenInclude(p => p.Owner)
                .Include(a => a.Service)
                .Include(a => a.Veterinarian)
                    .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task<ServiceResult<AppointmentDto>> GetAppointmentDtoAsync(int appointmentId, string userId, string userRole)
        {
            // Görünürlük kuralı aynı (VisibleTo); entity yerine Select ile DTO'ya eşlenir.
            // EnumTexts.ToTurkish SQL'e çevrilemez; EF Core son Select'teki bu çağrıyı satırlar geldikten sonra C#'ta çalıştırır.
            var appointment = await VisibleTo(userId, userRole)
                .AsNoTracking()
                .Where(a => a.Id == appointmentId)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    PetId = a.PetId,
                    PetName = a.Pet.Name,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service.Name,
                    VeterinarianId = a.VeterinarianId,
                    VeterinarianName = a.Veterinarian.Title + " " + a.Veterinarian.User.FullName,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    DurationMinutes = a.DurationMinutes,
                    Price = a.Price,
                    Status = EnumTexts.ToTurkish(a.Status),
                    RejectionReason = a.RejectionReason,
                    ExaminationNote = a.ExaminationNote,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return ServiceResult<AppointmentDto>.NotFound("Randevu bulunamadı.");
            }

            return ServiceResult<AppointmentDto>.Ok(appointment);
        }

        public bool CanCancel(Appointment appointment)
        {
            return CheckCancelRules(appointment, DateTime.Now).Success;
        }

        public bool CanComplete(Appointment appointment)
        {
            return CheckCompleteRules(appointment, DateTime.Now).Success;
        }

        public async Task<ServiceResult<bool>> ApproveAsync(int appointmentId, string userId, string userRole)
        {
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            // Üye kendi randevusunu görebilir ama onaylayamaz: kayıt görünüyor, işlem yetkisi yok (Forbidden).
            if (userRole != AppRoles.Admin && userRole != AppRoles.Veterinarian)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca randevunun hekimi ya da yönetici onaylayabilir.");
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                return ServiceResult<bool>.Validation(
                    $"Yalnızca \"Bekliyor\" durumundaki randevu onaylanabilir; bu randevu \"{EnumTexts.ToTurkish(appointment.Status)}\".");
            }

            appointment.Status = AppointmentStatus.Approved;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> RejectAsync(int appointmentId, string userId, string userRole, string? reason)
        {
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            if (userRole != AppRoles.Admin && userRole != AppRoles.Veterinarian)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca randevunun hekimi ya da yönetici reddedebilir.");
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                return ServiceResult<bool>.Validation(
                    $"Yalnızca \"Bekliyor\" durumundaki randevu reddedilebilir; bu randevu \"{EnumTexts.ToTurkish(appointment.Status)}\".");
            }

            // Formdaki [Required] kullanıcıyı göndermeden uyarır; asıl kural burada, çünkü servis hangi arayüzden çağrılırsa çağrılsın geçerli.
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult<bool>.Validation("Ret nedeni zorunludur.");
            }

            appointment.Status = AppointmentStatus.Rejected;
            appointment.RejectionReason = reason.Trim();
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> CompleteAsync(int appointmentId, string userId, string userRole, string? examinationNote)
        {
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            // Muayeneyi yapan hekim not girer; yönetici de üye de tamamlayamaz.
            if (userRole != AppRoles.Veterinarian)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca randevunun hekimi tamamlayabilir.");
            }

            var check = CheckCompleteRules(appointment, DateTime.Now);
            if (!check.Success)
            {
                return check;
            }

            if (string.IsNullOrWhiteSpace(examinationNote))
            {
                return ServiceResult<bool>.Validation("Muayene notu zorunludur.");
            }

            appointment.Status = AppointmentStatus.Completed;
            appointment.ExaminationNote = examinationNote.Trim();
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> CancelAsync(int appointmentId, string userId, string userRole)
        {
            // Kaydı göremeyen kullanıcıya (başkasının randevusu) "bulunamadı" denir.
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            // Hekim kendi randevusunu görebilir ama iptal edemez: kayıt görünüyor, işlem yetkisi yok (Forbidden).
            if (userRole != AppRoles.Member && userRole != AppRoles.Admin)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca hayvanın sahibi ya da yönetici iptal edebilir.");
            }

            var check = CheckCancelRules(appointment, DateTime.Now);
            if (!check.Success)
            {
                return check;
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(int appointmentId, string userId, string userRole,
            int veterinarianId, DateTime startTime)
        {
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            if (userRole != AppRoles.Admin)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca yönetici düzenleyebilir.");
            }

            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Approved)
            {
                return ServiceResult<bool>.Validation(
                    $"\"{EnumTexts.ToTurkish(appointment.Status)}\" durumundaki randevu düzenlenemez.");
            }

            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == appointment.ServiceId);
            if (service == null)
            {
                return ServiceResult<bool>.NotFound("Hizmet bulunamadı.");
            }

            // 2-7. CreateAsync ile aynı kontroller. Süre olarak randevuda saklanan süre kullanılır (hizmetin bugünkü süresi değil)
            // ve randevunun kendisi çakışma hesabına girmez.
            var check = await CheckBookingRulesAsync(service, appointment.PetId, veterinarianId, startTime,
                appointment.DurationMinutes, appointment.Id);
            if (!check.Success)
            {
                return check;
            }

            // Hizmet, süre ve ücret değişmez; yalnızca hekim ve saat değişir, bitiş saklanan süreden yeniden hesaplanır.
            appointment.VeterinarianId = veterinarianId;
            appointment.StartTime = startTime;
            appointment.EndTime = startTime.AddMinutes(appointment.DurationMinutes);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int appointmentId, string userId, string userRole)
        {
            var appointment = await FindVisibleAsync(appointmentId, userId, userRole);
            if (appointment == null)
            {
                return ServiceResult<bool>.NotFound("Randevu bulunamadı.");
            }

            if (userRole != AppRoles.Admin)
            {
                return ServiceResult<bool>.Forbidden("Randevuyu yalnızca yönetici silebilir.");
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        // İptal kuralı tek yerde: yalnızca Bekliyor ya da Onaylandı durumundaki, başlangıcı gelmemiş randevu iptal edilir.
        // Geçersiz durum geçişi ve zaman kuralı ihlali Validation'dır (Conflict yalnızca zaman çakışmaları içindir).
        private static ServiceResult<bool> CheckCancelRules(Appointment appointment, DateTime now)
        {
            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Approved)
            {
                return ServiceResult<bool>.Validation(
                    $"\"{EnumTexts.ToTurkish(appointment.Status)}\" durumundaki randevu iptal edilemez.");
            }

            if (appointment.StartTime <= now)
            {
                return ServiceResult<bool>.Validation("Başlangıç saati geçmiş randevu iptal edilemez.");
            }

            return ServiceResult<bool>.Ok(true);
        }

        // Tamamlama kuralı tek yerde: yalnızca Onaylandı durumundaki ve başlangıç saati gelmiş randevu tamamlanır.
        // CompleteAsync uygular; hekim panelindeki tamamlama formu da CanComplete ile buna sorar.
        private static ServiceResult<bool> CheckCompleteRules(Appointment appointment, DateTime now)
        {
            if (appointment.Status != AppointmentStatus.Approved)
            {
                return ServiceResult<bool>.Validation(
                    $"Yalnızca \"Onaylandı\" durumundaki randevu tamamlanabilir; bu randevu \"{EnumTexts.ToTurkish(appointment.Status)}\".");
            }

            if (appointment.StartTime > now)
            {
                return ServiceResult<bool>.Validation("Başlangıç saati gelmemiş randevu tamamlanamaz.");
            }

            return ServiceResult<bool>.Ok(true);
        }

        // Görünürlük kuralı tek yerde: Admin tüm randevuları, Üye yalnızca kendi hayvanlarının, Veteriner yalnızca kendi
        // randevularını görür. Sorguya eklenen bir Where olduğu için SQL'de çalışır. Listeler, detaylar ve durum değiştiren
        // tüm işlemler randevuyu bunun üzerinden bulur; göremediği randevu kullanıcı için "yok"tur.
        private IQueryable<Appointment> VisibleTo(string userId, string userRole)
        {
            if (userRole == AppRoles.Admin)
            {
                return _context.Appointments;
            }

            if (userRole == AppRoles.Member)
            {
                return _context.Appointments.Where(a => a.Pet.OwnerId == userId);
            }

            if (userRole == AppRoles.Veterinarian)
            {
                return _context.Appointments.Where(a => a.Veterinarian.UserId == userId);
            }

            // Tanınmayan rol hiçbir randevuyu göremez.
            return _context.Appointments.Where(a => false);
        }

        // Durum değiştiren işlemler randevuyu bununla yükler. AsNoTracking yok: işlem randevuyu değiştirip kaydedecek.
        private async Task<Appointment?> FindVisibleAsync(int appointmentId, string userId, string userRole)
        {
            return await VisibleTo(userId, userRole).FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        // 2-7 numaralı kontroller. CreateAsync ve UpdateAsync ikisi de bunu çağırır; kurallar ikinci bir yerde yazılmaz.
        // durationMinutes: yeni randevuda hizmetin süresi, düzenlemede randevuda saklanan süre.
        // excludedAppointmentId: düzenlemede randevunun kendisi 6 ve 7'deki çakışma hesabına girmesin (yeni randevuda 0).
        private async Task<ServiceResult<bool>> CheckBookingRulesAsync(Service service, int petId, int veterinarianId,
            DateTime startTime, int durationMinutes, int excludedAppointmentId)
        {
            // 2. Hekim var mı (yoksa NotFound), hizmet aktif mi ve hekim bu hizmeti veriyor mu (Validation)?
            var veterinarian = await _context.Veterinarians
                .AsNoTracking()
                .Include(v => v.Services)
                .Include(v => v.Availabilities)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.Id == veterinarianId);
            if (veterinarian == null)
            {
                return ServiceResult<bool>.NotFound("Hekim bulunamadı.");
            }

            if (!service.IsActive)
            {
                return ServiceResult<bool>.Validation("Bu hizmet şu anda randevuya açık değil.");
            }

            if (!veterinarian.Services.Any(s => s.Id == service.Id))
            {
                return ServiceResult<bool>.Validation("Seçilen hekim bu hizmeti vermiyor.");
            }

            // 3-6. Ortak kontrol metodu. Bitiş saati formdan alınmaz, süreden hesaplanır.
            var endTime = startTime.AddMinutes(durationMinutes);
            var date = DateOnly.FromDateTime(startTime);
            var workingHour = await _context.ClinicWorkingHours.AsNoTracking()
                .FirstOrDefaultAsync(h => h.DayOfWeek == date.DayOfWeek);
            var veterinarianAppointments = (await GetVeterinarianAppointmentsOnDayAsync(new List<int> { veterinarianId }, date))
                .Where(a => a.Id != excludedAppointmentId)
                .ToList();

            var check = CheckTimeRules(startTime, endTime, DateTime.Now, workingHour, veterinarian.Availabilities, veterinarianAppointments);
            if (!check.Success)
            {
                return check;
            }

            // 7. Aynı hayvanın aynı aralıkta başka bir randevusu var mı? (Başka bir hekimde olsa bile hayvan iki yerde olamaz.)
            var petAppointments = (await GetPetAppointmentsOnDayAsync(petId, date))
                .Where(a => a.Id != excludedAppointmentId)
                .ToList();
            var petConflict = FindOverlapping(petAppointments, startTime, endTime);
            if (petConflict != null)
            {
                return ServiceResult<bool>.Conflict(
                    $"Bu hayvanın {petConflict.StartTime:HH:mm}-{petConflict.EndTime:HH:mm} arasında başka bir randevusu var.");
            }

            return ServiceResult<bool>.Ok(true);
        }

        // Ortak kontrol metodu: 3-6 numaralı kurallar yalnızca burada yazılır.
        // Veritabanına gitmez; gereken her şey parametre olarak gelir. GetAvailableSlotsAsync bunu her aday saat için,
        // CheckBookingRulesAsync (CreateAsync ve UpdateAsync) tek bir aralık için çağırır.
        // "now" da parametredir ki tüm adaylar aynı ana göre denensin.
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
