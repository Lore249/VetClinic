using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    // Üyenin randevu işlemleri. Bütün kurallar IAppointmentService'te; controller yalnızca girdiyi alır ve sonucu gösterir.
    [Authorize(Roles = AppRoles.Member)]
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPetService _petService;
        private readonly IServiceCatalogService _serviceCatalog;

        public AppointmentsController(IAppointmentService appointmentService, IPetService petService,
            IServiceCatalogService serviceCatalog)
        {
            _appointmentService = appointmentService;
            _petService = petService;
            _serviceCatalog = serviceCatalog;
        }

        // Kullanıcı id'si her zaman oturumdaki kimlikten okunur.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Randevularım
        public async Task<IActionResult> Index()
        {
            var appointments = await _appointmentService.GetMyAppointmentsAsync(CurrentUserId);
            return View(appointments);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Başkasının randevusunun id'si yazılırsa servis null döner ve 404 gösterilir.
            var appointment = await _appointmentService.GetMyAppointmentAsync(CurrentUserId, id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(BuildDetails(appointment));
        }

        // 1. adım (GET): /Appointments/Create?PetId=1&ServiceId=2&Date=2026-11-24
        // Hizmet ve tarih seçilmişse o günün boş saatleri sunucuda hesaplanıp sayfayla birlikte gönderilir.
        [HttpGet]
        public async Task<IActionResult> Create(int? petId, int? serviceId, DateOnly? date)
        {
            var model = new AppointmentCreateViewModel
            {
                PetId = petId,
                ServiceId = serviceId,
                Date = date ?? DateOnly.FromDateTime(DateTime.Today)
            };

            // FillPageAsync null dönerse sayfa gösterilir; değilse (ör. olmayan bir hizmet için 404) o sonuç döner.
            return await FillPageAsync(model) ?? View(model);
        }

        // 2. adım (POST): bir saat düğmesine tıklanınca. Hayvan, hizmet ve tarih formdaki gizli alanlardan;
        // hekim ve başlangıç saati düğmenin formaction adresindeki sorgu dizesinden gelir. Model binding ikisini de okur.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Alanlar [Required] olduğu için ModelState geçerliyse doludur (! ve .Value bu yüzden güvenli).
                var result = await _appointmentService.CreateAsync(CurrentUserId, model.PetId!.Value, model.ServiceId!.Value,
                    model.VeterinarianId!.Value, model.StartTime!.Value);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Randevunuz oluşturuldu ve onay bekliyor.";
                    return RedirectToAction(nameof(Details), new { id = result.Data });
                }

                // Eşleme tablosu: NotFound -> 404; Validation ve Conflict -> sayfada hata mesajı.
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            // Hata varsa (ör. saat bu arada başkası tarafından alındıysa) aynı arama yeniden gösterilir;
            // boş saatler güncel hâliyle yeniden hesaplandığı için dolan saat listeden kalkar.
            return await FillPageAsync(model) ?? View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // Bu controller'a yalnızca Üye girebildiği için rol sabittir; sahiplik ve iptal kuralını servis kendisi kontrol eder.
            var result = await _appointmentService.CancelAsync(id, CurrentUserId, AppRoles.Member);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Randevu iptal edildi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var errorResult = this.HandleServiceError(result);
            if (errorResult != null)
            {
                return errorResult;
            }

            // İptal kuralına takıldıysa (ör. başlangıç saati geçmişse) detay sayfası hata mesajıyla gösterilir.
            var appointment = await _appointmentService.GetMyAppointmentAsync(CurrentUserId, id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(nameof(Details), BuildDetails(appointment));
        }

        // Randevu ekranının seçeneklerini ve (hizmet ile tarih seçiliyse) boş saatleri doldurur.
        // Boş saatler alınamazsa eşleme sonucunu döner (olmayan hizmet için 404); null dönerse sayfa gösterilebilir.
        private async Task<IActionResult?> FillPageAsync(AppointmentCreateViewModel model)
        {
            model.PetOptions = await _petService.GetMyPetsAsync(CurrentUserId);
            model.ServiceOptions = await _serviceCatalog.GetActiveAsync();

            if (model.ServiceId == null || model.Date == null)
            {
                return null;
            }

            var result = await _appointmentService.GetAvailableSlotsAsync(model.ServiceId.Value, model.Date.Value);
            if (result.Success)
            {
                model.Slots = result.Data;
                return null;
            }

            return this.HandleServiceError(result);
        }

        private AppointmentDetailsViewModel BuildDetails(Appointment appointment)
        {
            return new AppointmentDetailsViewModel
            {
                Appointment = appointment,
                CanCancel = _appointmentService.CanCancel(appointment)
            };
        }
    }
}
