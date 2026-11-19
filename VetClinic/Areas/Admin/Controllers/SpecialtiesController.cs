using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class SpecialtiesController : Controller
    {
        private readonly ISpecialtyService _specialtyService;

        public SpecialtiesController(ISpecialtyService specialtyService)
        {
            _specialtyService = specialtyService;
        }

        public async Task<IActionResult> Index()
        {
            var specialties = await _specialtyService.GetAllAsync();
            return View(specialties);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SpecialtyFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpecialtyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _specialtyService.CreateAsync(model);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["SuccessMessage"] = "Uzmanlık eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var specialty = await _specialtyService.GetByIdAsync(id);
            if (specialty == null)
            {
                return NotFound();
            }

            var model = new SpecialtyFormViewModel
            {
                Id = specialty.Id,
                Name = specialty.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SpecialtyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var error = await _specialtyService.UpdateAsync(model);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            TempData["SuccessMessage"] = "Uzmanlık güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // Silme iki adımlıdır: GET onay sayfasını gösterir, asıl silme POST ile yapılır.
        // Böylece bir bağlantıya tıklamak ya da adresi açmak kaydı silmez.
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var specialty = await _specialtyService.GetByIdAsync(id);
            if (specialty == null)
            {
                return NotFound();
            }

            return View(specialty);
        }

        // GET ve POST metotları aynı parametreyi (int id) aldığı için C#'ta aynı adı taşıyamaz;
        // ActionName sayesinde bu metot da /Delete adresine bağlanır.
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var error = await _specialtyService.DeleteAsync(id);
            if (error != null)
            {
                var specialty = await _specialtyService.GetByIdAsync(id);
                if (specialty == null)
                {
                    return NotFound();
                }

                ModelState.AddModelError(string.Empty, error);
                return View(specialty);
            }

            TempData["SuccessMessage"] = "Uzmanlık silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
