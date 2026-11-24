using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Areas.Admin.Controllers
{
    // Admin randevu yönetimi: tüm randevuları görür, onaylar, reddeder, iptal eder, düzenler ve siler.
    // Rol her çağrıda Admin olarak geçilir; kurallar ve yetki kontrolü IAppointmentService'te.
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IVeterinarianService _veterinarianService;

        public AppointmentsController(IAppointmentService appointmentService, IVeterinarianService veterinarianService)
        {
            _appointmentService = appointmentService;
            _veterinarianService = veterinarianService;
        }

        // Kullanıcı id'si her zaman oturumdaki kimlikten okunur.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(CurrentUserId, AppRoles.Admin);
            return View(appointments);
        }

        public async Task<IActionResult> Details(int id)
        {
            return await ShowDetailsAsync(id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _appointmentService.ApproveAsync(id, CurrentUserId, AppRoles.Admin);
            return await AfterActionAsync(result, id, "Randevu onaylandı.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(RejectAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await ShowDetailsAsync(model.AppointmentId);
            }

            var result = await _appointmentService.RejectAsync(model.AppointmentId, CurrentUserId, AppRoles.Admin, model.Reason);
            return await AfterActionAsync(result, model.AppointmentId, "Randevu reddedildi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _appointmentService.CancelAsync(id, CurrentUserId, AppRoles.Admin);
            return await AfterActionAsync(result, id, "Randevu iptal edildi.");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(id, CurrentUserId, AppRoles.Admin);
            if (appointment == null)
            {
                return NotFound();
            }

            var model = new AppointmentEditViewModel
            {
                Id = appointment.Id,
                VeterinarianId = appointment.VeterinarianId,
                StartTime = appointment.StartTime
            };

            await FillEditPageAsync(model, appointment);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppointmentEditViewModel model)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(model.Id, CurrentUserId, AppRoles.Admin);
            if (appointment == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _appointmentService.UpdateAsync(model.Id, CurrentUserId, AppRoles.Admin,
                    model.VeterinarianId!.Value, model.StartTime!.Value);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Randevu güncellendi.";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }

                // Çakışma, müsaitlik ya da çalışma saati hatası formda gösterilir.
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            await FillEditPageAsync(model, appointment);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(id, CurrentUserId, AppRoles.Admin);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _appointmentService.DeleteAsync(id, CurrentUserId, AppRoles.Admin);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Randevu silindi.";
                return RedirectToAction(nameof(Index));
            }

            var errorResult = this.HandleServiceError(result);
            if (errorResult != null)
            {
                return errorResult;
            }

            var appointment = await _appointmentService.GetAppointmentAsync(id, CurrentUserId, AppRoles.Admin);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // İşlem sonrası ortak akış: başarılıysa mesajla detay sayfasına dön (Post-Redirect-Get);
        // 404/403 ise onu döndür; kural ihlalinde detay sayfasını hata mesajıyla yeniden göster.
        private async Task<IActionResult> AfterActionAsync(ServiceResult<bool> result, int appointmentId, string successMessage)
        {
            if (result.Success)
            {
                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction(nameof(Details), new { id = appointmentId });
            }

            return this.HandleServiceError(result) ?? await ShowDetailsAsync(appointmentId);
        }

        private async Task<IActionResult> ShowDetailsAsync(int appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(appointmentId, CurrentUserId, AppRoles.Admin);
            if (appointment == null)
            {
                return NotFound();
            }

            var model = new AppointmentDetailsViewModel
            {
                Appointment = appointment,
                CanCancel = _appointmentService.CanCancel(appointment)
            };

            return View(nameof(Details), model);
        }

        // Düzenleme formunda yalnızca randevunun hizmetini veren hekimler listelenir (kolaylık için);
        // başka bir hekim gönderilirse servis "Seçilen hekim bu hizmeti vermiyor." der.
        private async Task FillEditPageAsync(AppointmentEditViewModel model, Appointment appointment)
        {
            model.Appointment = appointment;
            var veterinarians = await _veterinarianService.GetAllAsync();
            model.VeterinarianOptions = veterinarians
                .Where(v => v.Services.Any(s => s.Id == appointment.ServiceId))
                .ToList();
        }
    }
}
