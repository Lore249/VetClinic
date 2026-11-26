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
            // Rol olarak Üye verilir: servis yalnızca bu kullanıcının hayvanlarının randevularını döndürür.
            var appointments = await _appointmentService.GetAppointmentsAsync(CurrentUserId, AppRoles.Member);
            return View(appointments);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Başkasının randevusunun id'si yazılırsa servis null döner ve 404 gösterilir.
            var appointment = await _appointmentService.GetAppointmentAsync(id, CurrentUserId, AppRoles.Member);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(BuildDetails(appointment));
        }

        // 1. adım (GET): sayfa ve seçim kutuları. Seçimler adreste varsa (/Appointments/Create?PetId=1&ServiceId=2&Date=2026-11-24)
        // form dolu açılır. Boş saatleri sunucu hesaplamaz; sayfadaki betik (wwwroot/js/appointment-slots.js) REST API'den alır.
        [HttpGet]
        public async Task<IActionResult> Create(int? petId, int? serviceId, DateOnly? date)
        {
            var model = new AppointmentCreateViewModel
            {
                PetId = petId,
                ServiceId = serviceId,
                Date = date ?? DateOnly.FromDateTime(DateTime.Today)
            };

            await FillOptionsAsync(model);
            return View(model);
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

            // Hata varsa (ör. saat bu arada başkası tarafından alındıysa) sayfa aynı seçimlerle ve hata mesajıyla yeniden açılır;
            // betik boş saatleri API'den yeniden aldığı için dolan saat listede görünmez.
            await FillOptionsAsync(model);
            return View(model);
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
            var appointment = await _appointmentService.GetAppointmentAsync(id, CurrentUserId, AppRoles.Member);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(nameof(Details), BuildDetails(appointment));
        }

        // Randevu ekranının seçim kutuları: üyenin hayvanları ve aktif hizmetler.
        private async Task FillOptionsAsync(AppointmentCreateViewModel model)
        {
            model.PetOptions = await _petService.GetMyPetsAsync(CurrentUserId);
            model.ServiceOptions = await _serviceCatalog.GetActiveAsync();
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
