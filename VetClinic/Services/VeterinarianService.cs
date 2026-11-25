using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class VeterinarianService : IVeterinarianService
    {
        private const string NoServiceSelectedError = "En az bir hizmet seçilmelidir; hizmet vermeyen hekime randevu alınamaz.";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VeterinarianService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<Veterinarian>> GetAllAsync()
        {
            // Hekimin adı ApplicationUser'da olduğu için User da yüklenir.
            // AsSplitQuery: iki koleksiyon (uzmanlık ve hizmet) tek sorguda yüklenirse her uzmanlık her hizmetle
            // eşleşip satırlar çoğalır; ayrı sorgularla yüklemek bunu önler.
            return await _context.Veterinarians
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.Specialties)
                .Include(v => v.Services)
                .AsSplitQuery()
                .OrderBy(v => v.User.FullName)
                .ToListAsync();
        }

        public async Task<Veterinarian?> GetByIdAsync(int id)
        {
            return await _context.Veterinarians
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.Specialties)
                .Include(v => v.Services)
                .Include(v => v.Availabilities)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<ServiceResult<List<VeterinarianDto>>> GetPublicListAsync(int? specialtyId)
        {
            if (specialtyId != null && !await _context.Specialties.AnyAsync(s => s.Id == specialtyId))
            {
                return ServiceResult<List<VeterinarianDto>>.NotFound("Uzmanlık bulunamadı.");
            }

            var query = _context.Veterinarians.AsNoTracking();

            // LINQ filtresi: uzmanlık verildiyse yalnızca o uzmanlığa sahip hekimler. Where SQL'e (EXISTS) çevrilir.
            if (specialtyId != null)
            {
                query = query.Where(v => v.Specialties.Any(s => s.Id == specialtyId));
            }

            // Entity yerine DTO: Select ile yalnızca gereken alanlar seçilir, SQL de yalnızca bu sütunları getirir.
            var veterinarians = await query
                .OrderBy(v => v.User.FullName)
                .Select(v => new VeterinarianDto
                {
                    Id = v.Id,
                    Title = v.Title,
                    FullName = v.User.FullName,
                    Specialties = v.Specialties
                        .OrderBy(s => s.Name)
                        .Select(s => new IdNameDto { Id = s.Id, Name = s.Name })
                        .ToList(),
                    Services = v.Services
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Name)
                        .Select(s => new IdNameDto { Id = s.Id, Name = s.Name })
                        .ToList()
                })
                .AsSplitQuery()
                .ToListAsync();

            return ServiceResult<List<VeterinarianDto>>.Ok(veterinarians);
        }

        public async Task<ServiceResult<int>> CreateAsync(VeterinarianCreateViewModel model)
        {
            // Formdan gelen id'lerle veritabanındaki kayıtlar alınır; var olmayan bir id gönderilirse kendiliğinden elenir.
            var services = await _context.Services.Where(s => model.SelectedServiceIds.Contains(s.Id)).ToListAsync();
            if (services.Count == 0)
            {
                return ServiceResult<int>.Validation(NoServiceSelectedError);
            }

            var specialties = await _context.Specialties.Where(s => model.SelectedSpecialtyIds.Contains(s.Id)).ToListAsync();

            // Hesap ve hekim kaydı birlikte açılır. Hekim kaydı eklenemezse hesap da geri alınır; böylece hekim kaydı
            // olmayan bir Veteriner hesabı kalmaz. UserManager da aynı DbContext'i kullandığı için aynı transaction'dadır.
            // Commit'e ulaşmadan metottan çıkılırsa (return) using bloğu transaction'ı kapatır ve her şey geri alınır.
            using var transaction = await _context.Database.BeginTransactionAsync();

            var email = model.Email.Trim();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                // Aynı e-posta gibi hatalar TurkishIdentityErrorDescriber sayesinde Türkçe gelir.
                return ServiceResult<int>.Validation(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, AppRoles.Veterinarian);

            var veterinarian = new Veterinarian
            {
                Title = model.Title.Trim(),
                UserId = user.Id,
                Specialties = specialties,
                Services = services
            };
            _context.Veterinarians.Add(veterinarian);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return ServiceResult<int>.Ok(veterinarian.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(VeterinarianFormViewModel model)
        {
            // Bağlar değiştirileceği için mevcut uzmanlık ve hizmetler de yüklenir (AsNoTracking yok: EF değişiklikleri izlemeli).
            var veterinarian = await _context.Veterinarians
                .Include(v => v.User)
                .Include(v => v.Specialties)
                .Include(v => v.Services)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.Id == model.Id);
            if (veterinarian == null)
            {
                return ServiceResult<bool>.NotFound("Hekim bulunamadı.");
            }

            var services = await _context.Services.Where(s => model.SelectedServiceIds.Contains(s.Id)).ToListAsync();
            if (services.Count == 0)
            {
                return ServiceResult<bool>.Validation(NoServiceSelectedError);
            }

            var specialties = await _context.Specialties.Where(s => model.SelectedSpecialtyIds.Contains(s.Id)).ToListAsync();

            veterinarian.Title = model.Title.Trim();
            veterinarian.User.FullName = model.FullName.Trim();

            // Çoka çok bağlar: liste boşaltılıp seçilenler eklenir. EF kaydederken eski ve yeni listeyi karşılaştırır,
            // ara tabloya yalnızca farkı (kaldırılan ve yeni eklenen satırları) yazar.
            veterinarian.Specialties.Clear();
            foreach (var specialty in specialties)
            {
                veterinarian.Specialties.Add(specialty);
            }

            veterinarian.Services.Clear();
            foreach (var service in services)
            {
                veterinarian.Services.Add(service);
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var veterinarian = await _context.Veterinarians
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (veterinarian == null)
            {
                return ServiceResult<bool>.NotFound("Hekim bulunamadı.");
            }

            // İlişki Restrict olduğu için randevusu olan hekim silinmeye çalışılırsa veritabanı hata verir; önce kontrol edilir.
            if (await _context.Appointments.AnyAsync(a => a.VeterinarianId == id))
            {
                return ServiceResult<bool>.Validation("Bu hekimin randevuları olduğu için silinemez. Geçmiş randevular hekim bilgisiyle birlikte saklanmalıdır.");
            }

            // Hekim kaydı ve giriş hesabı birlikte silinir; biri başarısız olursa diğeri de geri alınır.
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Müsaitlik aralıkları ile uzmanlık ve hizmet bağları veritabanında Cascade ile hekimle birlikte silinir.
            _context.Veterinarians.Remove(veterinarian);
            await _context.SaveChangesAsync();

            // Hesabın rol kayıtları (AspNetUserRoles) da veritabanında Cascade ile silinir.
            var result = await _userManager.DeleteAsync(veterinarian.User);
            if (!result.Succeeded)
            {
                return ServiceResult<bool>.Validation(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<int>> AddAvailabilityAsync(AvailabilityViewModel model)
        {
            if (!await _context.Veterinarians.AnyAsync(v => v.Id == model.VeterinarianId))
            {
                return ServiceResult<int>.NotFound("Hekim bulunamadı.");
            }

            if (model.StartTime >= model.EndTime)
            {
                return ServiceResult<int>.Validation("Başlangıç saati bitiş saatinden önce olmalıdır.");
            }

            // Aynı hekimin aynı gündeki aralıkları çakışamaz. Koşul randevu çakışmasıyla aynıdır:
            // yeniBaşlangıç < mevcutBitiş ve mevcutBaşlangıç < yeniBitiş.
            // Uç uca gelen aralıklar (12.30'da biten ve 12.30'da başlayan) çakışma sayılmaz.
            var conflict = await _context.VeterinarianAvailabilities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.VeterinarianId == model.VeterinarianId
                    && a.DayOfWeek == model.DayOfWeek
                    && model.StartTime < a.EndTime
                    && a.StartTime < model.EndTime);

            // Müsaitlik aralıklarının çakışması bir kural ihlalidir (Validation); Conflict yalnızca randevu çakışmaları içindir.
            if (conflict != null)
            {
                return ServiceResult<int>.Validation("Bu aralık, hekimin " + EnumTexts.ToTurkish(conflict.DayOfWeek) + " "
                    + conflict.StartTime.ToString("HH:mm") + "-" + conflict.EndTime.ToString("HH:mm")
                    + " aralığıyla çakışıyor.");
            }

            var availability = new VeterinarianAvailability
            {
                VeterinarianId = model.VeterinarianId,
                DayOfWeek = model.DayOfWeek,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            };
            _context.VeterinarianAvailabilities.Add(availability);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(availability.Id);
        }

        public async Task<ServiceResult<bool>> DeleteAvailabilityAsync(int veterinarianId, int availabilityId)
        {
            // Hekim id'si de aranır: aralık yalnızca açık olan hekimin sayfasından silinebilsin.
            var availability = await _context.VeterinarianAvailabilities
                .FirstOrDefaultAsync(a => a.Id == availabilityId && a.VeterinarianId == veterinarianId);
            if (availability == null)
            {
                return ServiceResult<bool>.NotFound("Müsaitlik aralığı bulunamadı.");
            }

            _context.VeterinarianAvailabilities.Remove(availability);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
    }
}
