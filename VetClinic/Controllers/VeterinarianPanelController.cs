using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    // Hekim paneli: hekim yalnızca kendi randevularını görür, onaylar, reddeder ve tamamlar.
    // Rol her çağrıda Veteriner olarak geçilir; hangi randevuyu görebileceğine ve işlem yetkisine servis karar verir.
    [Authorize(Roles = AppRoles.Veterinarian)]
    public class VeterinarianPanelController : Controller
    {
        private readonly IAppointmentService _appointmentService;

        public VeterinarianPanelController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // Kullanıcı id'si her zaman oturumdaki kimlikten okunur.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(CurrentUserId, AppRoles.Veterinarian);
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
            var result = await _appointmentService.ApproveAsync(id, CurrentUserId, AppRoles.Veterinarian);
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

            var result = await _appointmentService.RejectAsync(model.AppointmentId, CurrentUserId, AppRoles.Veterinarian, model.Reason);
            return await AfterActionAsync(result, model.AppointmentId, "Randevu reddedildi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CompleteAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await ShowDetailsAsync(model.AppointmentId);
            }

            var result = await _appointmentService.CompleteAsync(model.AppointmentId, CurrentUserId, AppRoles.Veterinarian, model.ExaminationNote);
            return await AfterActionAsync(result, model.AppointmentId, "Randevu tamamlandı ve muayene notu kaydedildi.");
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

        // Detay sayfası. Formdan gelen hatalar ModelState'te durduğu için sayfa yeniden çizilince formların altında görünür.
        private async Task<IActionResult> ShowDetailsAsync(int appointmentId)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(appointmentId, CurrentUserId, AppRoles.Veterinarian);
            if (appointment == null)
            {
                return NotFound();
            }

            var model = new AppointmentDetailsViewModel
            {
                Appointment = appointment,
                CanComplete = _appointmentService.CanComplete(appointment)
            };

            return View(nameof(Details), model);
        }
    }
}
