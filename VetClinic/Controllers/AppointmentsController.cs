using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    // Üyenin randevu işlemleri. Bütün kurallar IAppointmentService'te; controller yalnızca formu alır ve sonucu gösterir.
    [Authorize(Roles = AppRoles.Member)]
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPetService _petService;
        private readonly IServiceCatalogService _serviceCatalog;
        private readonly IVeterinarianService _veterinarianService;

        public AppointmentsController(IAppointmentService appointmentService, IPetService petService,
            IServiceCatalogService serviceCatalog, IVeterinarianService veterinarianService)
        {
            _appointmentService = appointmentService;
            _petService = petService;
            _serviceCatalog = serviceCatalog;
            _veterinarianService = veterinarianService;
        }

        // Kullanıcı id'si her zaman oturumdaki kimlikten okunur.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Boş saatler: /Appointments/Slots?serviceId=1&date=2026-11-23 (GET formu; bir şey değiştirmediği için POST değil).
        [HttpGet]
        public async Task<IActionResult> Slots(AppointmentSlotsViewModel model)
        {
            model.ServiceOptions = await _serviceCatalog.GetAllAsync();

            if (model.Date == null)
            {
                model.Date = DateOnly.FromDateTime(DateTime.Today);
            }

            if (model.ServiceId != null)
            {
                var result = await _appointmentService.GetAvailableSlotsAsync(model.ServiceId.Value, model.Date.Value);
                if (result.Success)
                {
                    model.Slots = result.Data;
                }
                else
                {
                    var errorResult = this.HandleServiceError(result);
                    if (errorResult != null)
                    {
                        return errorResult;
                    }
                }
            }

            return View(model);
        }

        // Boş saatler sayfasındaki bir saate tıklanınca hizmet, hekim ve başlangıç adresten gelir ve form dolu açılır.
        [HttpGet]
        public async Task<IActionResult> Create(int? serviceId, int? veterinarianId, DateTime? startTime)
        {
            var model = new AppointmentCreateViewModel
            {
                ServiceId = serviceId,
                VeterinarianId = veterinarianId,
                StartTime = startTime
            };

            await FillOptionsAsync(model);
            return View(model);
        }

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
                    TempData["SuccessMessage"] = $"Randevu oluşturuldu ve onay bekliyor (randevu no: {result.Data}).";
                    return RedirectToAction(nameof(Create));
                }

                // Eşleme tablosu: NotFound -> 404; Validation ve Conflict -> formda hata mesajı.
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            await FillOptionsAsync(model);
            return View(model);
        }

        // Bu sade form kuralları denemek içindir: pasif hizmetler ve hizmeti vermeyen hekimler de bilerek seçilebilir,
        // böylece servisin bu durumlarda verdiği hata görülebilir.
        private async Task FillOptionsAsync(AppointmentCreateViewModel model)
        {
            model.PetOptions = await _petService.GetMyPetsAsync(CurrentUserId);
            model.ServiceOptions = await _serviceCatalog.GetAllAsync();
            model.VeterinarianOptions = await _veterinarianService.GetAllAsync();
        }
    }
}
