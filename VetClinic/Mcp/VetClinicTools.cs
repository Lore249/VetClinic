using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using ModelContextProtocol.Server;
using VetClinic.Helpers;
using VetClinic.Services;

namespace VetClinic.Mcp
{
    // MCP sunucusunun araçları. Bir yapay zekâ asistanı (MCP istemcisi) araçları adları ve Türkçe açıklamalarıyla görür,
    // uygun olanı seçip çağırır. Araçlar ince: girdiyi alır, MVC ve REST API'nin kullandığı servisleri çağırır, sonucu okunur
    // bir metne çevirir. İş kuralları burada değil, servislerde.
    // [McpServerToolType] bu sınıfın araç içerdiğini belirtir. Her araç çağrısında sınıftan yeni bir örnek oluşturulur ve
    // constructor'daki servisler DI'dan gelir (controller'lardaki gibi).
    // Araçların parametre adları Türkçedir (hizmetId, tarih...): MCP istemcisi argümanları bu adlarla gönderir.
    [McpServerToolType]
    public class VetClinicTools
    {
        private const string MemberOnlyMessage = "Bu araçlar yalnızca üye hesaplarıyla kullanılabilir.";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceCatalogService _serviceCatalog;
        private readonly IAppointmentService _appointmentService;
        private readonly IPetService _petService;
        private readonly IVaccinationService _vaccinationService;

        public VetClinicTools(IHttpContextAccessor httpContextAccessor, IServiceCatalogService serviceCatalog,
            IAppointmentService appointmentService, IPetService petService, IVaccinationService vaccinationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceCatalog = serviceCatalog;
            _appointmentService = appointmentService;
            _petService = petService;
            _vaccinationService = vaccinationService;
        }

        [McpServerTool(Name = "hizmetler", ReadOnly = true)]
        [Description("Kliniğin randevu alınabilen aktif hizmetlerini id, ad, süre ve ücretiyle listeler. Diğer araçlara verilecek hizmetId değeri buradan öğrenilir.")]
        public async Task<string> ListServices()
        {
            if (GetMemberUserId() == null)
            {
                return MemberOnlyMessage;
            }

            var services = await _serviceCatalog.GetActiveAsync();
            if (services.Count == 0)
            {
                return "Şu anda randevu alınabilen hizmet yok.";
            }

            var text = new StringBuilder("Aktif hizmetler (diğer araçlarda hizmetId olarak id kullanılır):\n");
            foreach (var service in services)
            {
                text.AppendLine($"- id {service.Id}: {service.Name}, {service.DurationMinutes} dk, {service.Price:N2} ₺");
            }

            return text.ToString();
        }

        [McpServerTool(Name = "uygun_saatler", ReadOnly = true)]
        [Description("Verilen günde, verilen hizmeti verebilen ve boş saati olan hekimleri (id ve adıyla) ve boş başlangıç saatlerini listeler. Randevu oluşturmadan önce kullanılır.")]
        public async Task<string> GetAvailableSlots(
            [Description("Hizmetin id'si (hizmetler aracından öğrenilir).")] int hizmetId,
            [Description("Tarih, yyyy-MM-dd biçiminde (örnek: 2026-11-20).")] string tarih)
        {
            if (GetMemberUserId() == null)
            {
                return MemberOnlyMessage;
            }

            // Tarih metin olarak gelir; biçimi tam olarak tutmazsa servise gidilmeden açıklama döner.
            if (!DateOnly.TryParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return $"'{tarih}' geçerli bir tarih değil. Tarihi yyyy-MM-dd biçiminde yazın (örnek: 2026-11-20).";
            }

            // Randevu formu ve REST API ile aynı metot: boş saatler aynı kurallarla hesaplanır.
            var result = await _appointmentService.GetAvailableSlotsAsync(hizmetId, date);
            if (!result.Success)
            {
                return "Boş saatler alınamadı. " + result.ToMcpMessage();
            }

            var dayText = date.ToString("dd.MM.yyyy dddd");
            if (result.Data!.Count == 0)
            {
                return $"{dayText} günü bu hizmet için uygun saat yok (klinik kapalı, hekimler o gün çalışmıyor ya da saatler dolu olabilir).";
            }

            var text = new StringBuilder($"{dayText} günü uygun hekimler ve boş başlangıç saatleri (randevu_olustur aracında veterinerId olarak id kullanılır):\n");
            foreach (var slot in result.Data)
            {
                var times = string.Join(", ", slot.StartTimes.Select(t => t.ToString("HH:mm")));
                text.AppendLine($"- id {slot.VeterinarianId}: {slot.VeterinarianName}: {times}");
            }

            return text.ToString();
        }

        [McpServerTool(Name = "asi_takvimi", ReadOnly = true)]
        [Description("Kullanıcının adı verilen hayvanının geçmiş, yaklaşan ve gecikmiş aşılarını döner. Bu adda hayvan yoksa kullanıcının kayıtlı hayvanlarının adlarını söyler.")]
        public async Task<string> GetVaccinationSchedule(
            [Description("Hayvanın adı; büyük/küçük harf fark etmez (örnek: Fıstık).")] string hayvanAdi)
        {
            var userId = GetMemberUserId();
            if (userId == null)
            {
                return MemberOnlyMessage;
            }

            var pet = await _petService.FindMyPetByNameAsync(userId, hayvanAdi);
            if (pet == null)
            {
                return await PetNotFoundMessageAsync(userId, hayvanAdi);
            }

            // REST API'deki GET /api/pets/{id}/vaccinations ile aynı metot.
            var result = await _vaccinationService.GetScheduleAsync(userId, pet.Id);
            if (!result.Success)
            {
                return "Aşı takvimi alınamadı. " + result.ToMcpMessage();
            }

            var schedule = result.Data!;
            var text = new StringBuilder($"{schedule.PetName} için aşı takvimi:\n");

            AppendSection(text, "Yaklaşan dozlar", schedule.Upcoming
                .Select(v => $"{v.VaccineName}: {v.NextDoseOn!.Value:dd.MM.yyyy}")
                .ToList());
            AppendSection(text, "Gecikmiş dozlar (tarihi geçmiş)", schedule.Overdue
                .Select(v => $"{v.VaccineName}: {v.NextDoseOn!.Value:dd.MM.yyyy}")
                .ToList());
            AppendSection(text, "Geçmiş aşılar", schedule.Past
                .Select(v => $"{v.VaccineName}: {v.AdministeredOn:dd.MM.yyyy}")
                .ToList());

            return text.ToString();
        }

        // Veri yazan araç: Destructive = false, çünkü yalnızca yeni kayıt ekler, var olan veriyi silmez ya da değiştirmez.
        [McpServerTool(Name = "randevu_olustur", Destructive = false)]
        [Description("Kullanıcının hayvanı için 'Bekliyor' durumunda randevu oluşturur; hekim ya da yönetici onaylayınca kesinleşir. Önce uygun_saatler ile boş bir hekim ve saat seçilmelidir. Randevu oluşturulamazsa nedenini açıklar.")]
        public async Task<string> CreateAppointment(
            [Description("Hayvanın adı; büyük/küçük harf fark etmez (örnek: Fıstık).")] string hayvanAdi,
            [Description("Hizmetin id'si (hizmetler aracından öğrenilir).")] int hizmetId,
            [Description("Hekimin id'si (uygun_saatler aracından öğrenilir).")] int veterinerId,
            [Description("Başlangıç tarihi ve saati, yyyy-MM-dd HH:mm biçiminde (örnek: 2026-11-20 14:30).")] string baslangic)
        {
            var userId = GetMemberUserId();
            if (userId == null)
            {
                return MemberOnlyMessage;
            }

            if (!DateTime.TryParseExact(baslangic, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
            {
                return $"'{baslangic}' geçerli bir başlangıç değil. yyyy-MM-dd HH:mm biçiminde yazın (örnek: 2026-11-20 14:30).";
            }

            var pet = await _petService.FindMyPetByNameAsync(userId, hayvanAdi);
            if (pet == null)
            {
                return await PetNotFoundMessageAsync(userId, hayvanAdi);
            }

            // Randevu formu ve REST API ile aynı metot: 7 kuralın hepsi burada da uygulanır, hata nedeni aynen aktarılır.
            var result = await _appointmentService.CreateAsync(userId, pet.Id, hizmetId, veterinerId, startTime);
            if (!result.Success)
            {
                return "Randevu oluşturulamadı. " + result.ToMcpMessage();
            }

            var created = await _appointmentService.GetAppointmentDtoAsync(result.Data, userId, AppRoles.Member);
            var appointment = created.Data!;

            return $"Randevu oluşturuldu (no: {appointment.Id}). Durum: {appointment.Status}. "
                + $"{appointment.PetName} · {appointment.ServiceName} · {appointment.VeterinarianName} · "
                + $"{appointment.StartTime:dd.MM.yyyy dddd HH:mm}-{appointment.EndTime:HH:mm} · {appointment.Price:N2} ₺. "
                + "Hekim ya da yönetici onaylayınca kesinleşir.";
        }

        [McpServerTool(Name = "randevularim", ReadOnly = true)]
        [Description("Kullanıcının yaklaşan randevularını tarih, saat, hayvan, hizmet, hekim ve durumlarıyla (Bekliyor, Onaylandı...) listeler.")]
        public async Task<string> ListMyAppointments()
        {
            var userId = GetMemberUserId();
            if (userId == null)
            {
                return MemberOnlyMessage;
            }

            // Görünürlük kuralı serviste: rol Üye verildiği için yalnızca bu kullanıcının hayvanlarının randevuları gelir.
            var appointments = await _appointmentService.GetAppointmentsAsync(userId, AppRoles.Member);
            var now = DateTime.Now;
            var upcoming = appointments
                .Where(a => a.StartTime > now)
                .OrderBy(a => a.StartTime)
                .ToList();

            if (upcoming.Count == 0)
            {
                return "Yaklaşan randevunuz yok.";
            }

            var text = new StringBuilder("Yaklaşan randevularınız:\n");
            foreach (var a in upcoming)
            {
                text.AppendLine($"- no {a.Id}: {a.StartTime:dd.MM.yyyy dddd HH:mm}-{a.EndTime:HH:mm} · {a.Pet.Name} · {a.Service.Name} · "
                    + $"{a.Veterinarian.Title} {a.Veterinarian.User.FullName} · {EnumTexts.ToTurkish(a.Status)}");
            }

            return text.ToString();
        }

        // Rol kontrolü tek yerde. Kullanıcı, /mcp isteğindeki JWT'den (HttpContext.User) tanınır; kullanıcı id'si hiçbir zaman
        // araç parametresinden alınmaz. Üye değilse (Admin, Veteriner) null döner ve araç MemberOnlyMessage metnini verir.
        private string? GetMemberUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.IsInRole(AppRoles.Member))
            {
                return null;
            }

            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Hayvan bulunamazsa asistan kullanıcıya doğru adı sorabilsin diye kayıtlı hayvanların adları söylenir.
        private async Task<string> PetNotFoundMessageAsync(string userId, string petName)
        {
            var pets = await _petService.GetMyPetsAsync(userId);
            if (pets.Count == 0)
            {
                return $"'{petName}' adında bir hayvanınız bulunamadı. Kayıtlı hayvanınız yok; önce web sitesinden hayvan ekleyin.";
            }

            return $"'{petName}' adında bir hayvanınız bulunamadı. Kayıtlı hayvanlarınız: {string.Join(", ", pets.Select(p => p.Name))}.";
        }

        // Başlık ve madde listesi; liste boşsa "yok" yazılır.
        private static void AppendSection(StringBuilder text, string title, List<string> lines)
        {
            text.AppendLine(title + ":");
            if (lines.Count == 0)
            {
                text.AppendLine("- yok");
            }

            foreach (var line in lines)
            {
                text.AppendLine("- " + line);
            }
        }
    }
}
