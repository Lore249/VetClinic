using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Areas.Admin.Controllers
{
    // Klinik bilgileri ve çalışma saatleri. Tek klinik olduğu için ekleme ve silme yok, yalnızca düzenleme var.
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class ClinicController : Controller
    {
        private readonly IClinicService _clinicService;

        public ClinicController(IClinicService clinicService)
        {
            _clinicService = clinicService;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var clinic = await _clinicService.GetClinicAsync();
            if (clinic == null)
            {
                return NotFound();
            }

            var model = new ClinicViewModel
            {
                Name = clinic.Name,
                Address = clinic.Address,
                Phone = clinic.Phone
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClinicViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _clinicService.UpdateClinicAsync(model);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            // Kayıttan sonra yönlendirme yapılır (Post-Redirect-Get); mesaj TempData ile bir sonraki isteğe taşınır.
            TempData["SuccessMessage"] = "Klinik bilgileri kaydedildi.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpGet]
        public async Task<IActionResult> WorkingHours()
        {
            var hours = await _clinicService.GetWorkingHoursAsync();

            var model = new WorkingHoursViewModel();
            foreach (var hour in hours)
            {
                model.Days.Add(new WorkingHourRowViewModel
                {
                    DayOfWeek = hour.DayOfWeek,
                    IsClosed = hour.IsClosed,
                    OpenTime = hour.OpenTime,
                    CloseTime = hour.CloseTime
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkingHours(WorkingHoursViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _clinicService.UpdateWorkingHoursAsync(model.Days);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["SuccessMessage"] = "Çalışma saatleri kaydedildi.";
            return RedirectToAction(nameof(WorkingHours));
        }
    }
}
