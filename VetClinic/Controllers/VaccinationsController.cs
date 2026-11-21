using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    // Aşı kayıtları hayvanın detay sayfasında listelenir; bu controller yalnızca ekleme, düzenleme ve silme yapar.
    [Authorize(Roles = AppRoles.Member)]
    public class VaccinationsController : Controller
    {
        private readonly IVaccinationService _vaccinationService;
        private readonly IPetService _petService;

        public VaccinationsController(IVaccinationService vaccinationService, IPetService petService)
        {
            _vaccinationService = vaccinationService;
            _petService = petService;
        }

        // Kullanıcı id'si her zaman oturumdaki kimlikten okunur (PetsController'daki gibi).
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // petId: aşının ekleneceği hayvan (/Vaccinations/Create?petId=3).
        [HttpGet]
        public async Task<IActionResult> Create(int petId)
        {
            var pet = await _petService.GetMyPetAsync(CurrentUserId, petId);
            if (pet == null)
            {
                return NotFound();
            }

            var model = new VaccinationFormViewModel
            {
                PetId = pet.Id,
                PetName = pet.Name,
                AdministeredOn = DateOnly.FromDateTime(DateTime.Today)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VaccinationFormViewModel model)
        {
            var pet = await _petService.GetMyPetAsync(CurrentUserId, model.PetId);
            if (pet == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var error = await _vaccinationService.CreateAsync(CurrentUserId, model);
                if (error == null)
                {
                    TempData["SuccessMessage"] = "Aşı kaydı eklendi.";
                    return RedirectToAction("Details", "Pets", new { id = pet.Id });
                }

                ModelState.AddModelError(string.Empty, error);
            }

            model.PetName = pet.Name;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _vaccinationService.GetMyRecordAsync(CurrentUserId, id);
            if (record == null)
            {
                return NotFound();
            }

            var model = new VaccinationFormViewModel
            {
                Id = record.Id,
                PetId = record.PetId,
                PetName = record.Pet.Name,
                VaccineName = record.VaccineName,
                AdministeredOn = record.AdministeredOn,
                NextDoseOn = record.NextDoseOn
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VaccinationFormViewModel model)
        {
            // Hangi hayvanın sayfasına dönüleceği formdan değil, veritabanındaki kayıttan alınır.
            var record = await _vaccinationService.GetMyRecordAsync(CurrentUserId, model.Id);
            if (record == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var error = await _vaccinationService.UpdateAsync(CurrentUserId, model);
                if (error == null)
                {
                    TempData["SuccessMessage"] = "Aşı kaydı güncellendi.";
                    return RedirectToAction("Details", "Pets", new { id = record.PetId });
                }

                ModelState.AddModelError(string.Empty, error);
            }

            model.PetId = record.PetId;
            model.PetName = record.Pet.Name;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _vaccinationService.GetMyRecordAsync(CurrentUserId, id);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _vaccinationService.GetMyRecordAsync(CurrentUserId, id);
            if (record == null)
            {
                return NotFound();
            }

            var error = await _vaccinationService.DeleteAsync(CurrentUserId, id);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(record);
            }

            TempData["SuccessMessage"] = "Aşı kaydı silindi.";
            return RedirectToAction("Details", "Pets", new { id = record.PetId });
        }
    }
}
