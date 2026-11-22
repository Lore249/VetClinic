using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Areas.Admin.Controllers
{
    // Hekim CRUD ve müsaitlik aralıkları. Hekim hesabını yalnızca Admin açar; herkese açık kayıt sayfası yalnızca Üye rolü verir.
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class VeterinariansController : Controller
    {
        private readonly IVeterinarianService _veterinarianService;
        private readonly ISpecialtyService _specialtyService;
        private readonly IServiceCatalogService _serviceCatalog;

        public VeterinariansController(IVeterinarianService veterinarianService, ISpecialtyService specialtyService,
            IServiceCatalogService serviceCatalog)
        {
            _veterinarianService = veterinarianService;
            _specialtyService = specialtyService;
            _serviceCatalog = serviceCatalog;
        }

        public async Task<IActionResult> Index()
        {
            var veterinarians = await _veterinarianService.GetAllAsync();
            return View(veterinarians);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new VeterinarianCreateViewModel();
            await FillOptionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VeterinarianCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _veterinarianService.CreateAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Hekim ve giriş hesabı oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }

                // NotFound/Forbidden ise o sonuç döner; diğer hatalar ModelState'e eklenir ve form aşağıda yeniden gösterilir.
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            // Form hatayla geri gösterilirken onay kutularının seçenekleri yeniden doldurulur (formdan gelmezler).
            await FillOptionsAsync(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var veterinarian = await _veterinarianService.GetByIdAsync(id);
            if (veterinarian == null)
            {
                return NotFound();
            }

            var model = new VeterinarianFormViewModel
            {
                Id = veterinarian.Id,
                FullName = veterinarian.User.FullName,
                Title = veterinarian.Title,
                SelectedSpecialtyIds = veterinarian.Specialties.Select(s => s.Id).ToList(),
                SelectedServiceIds = veterinarian.Services.Select(s => s.Id).ToList()
            };

            await FillOptionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VeterinarianFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _veterinarianService.UpdateAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Hekim bilgileri güncellendi.";
                    return RedirectToAction(nameof(Index));
                }

                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            await FillOptionsAsync(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var veterinarian = await _veterinarianService.GetByIdAsync(id);
            if (veterinarian == null)
            {
                return NotFound();
            }

            return View(veterinarian);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _veterinarianService.DeleteAsync(id);
            if (!result.Success)
            {
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }

                // Silinemediyse (örneğin randevusu varsa) onay sayfası hata mesajıyla yeniden gösterilir.
                var veterinarian = await _veterinarianService.GetByIdAsync(id);
                if (veterinarian == null)
                {
                    return NotFound();
                }

                return View(veterinarian);
            }

            TempData["SuccessMessage"] = "Hekim ve giriş hesabı silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Müsaitlik sayfası: hekimin aralıkları ve yeni aralık ekleme formu. id hekimin id'sidir.
        [HttpGet]
        public async Task<IActionResult> Availability(int id)
        {
            var veterinarian = await _veterinarianService.GetByIdAsync(id);
            if (veterinarian == null)
            {
                return NotFound();
            }

            // Formun ilk açılışta boş görünmemesi için yaygın bir aralık önerilir.
            var model = new AvailabilityViewModel
            {
                VeterinarianId = veterinarian.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            };

            FillAvailabilityList(model, veterinarian);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Availability(AvailabilityViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _veterinarianService.AddAvailabilityAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Müsaitlik aralığı eklendi.";
                    return RedirectToAction(nameof(Availability), new { id = model.VeterinarianId });
                }

                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }

            var veterinarian = await _veterinarianService.GetByIdAsync(model.VeterinarianId);
            if (veterinarian == null)
            {
                return NotFound();
            }

            FillAvailabilityList(model, veterinarian);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int veterinarianId, int availabilityId)
        {
            var result = await _veterinarianService.DeleteAvailabilityAsync(veterinarianId, availabilityId);
            if (!result.Success)
            {
                // Bu işlemde tek olası hata, aralığın bulunamamasıdır (404). Hatanın gösterileceği bir form olmadığı için
                // eşleme null dönse bile 404 verilir.
                return this.HandleServiceError(result) ?? NotFound();
            }

            TempData["SuccessMessage"] = "Müsaitlik aralığı silindi.";
            return RedirectToAction(nameof(Availability), new { id = veterinarianId });
        }

        // Ekleme ve düzenleme formundaki onay kutuları için tüm uzmanlık ve hizmetler.
        private async Task FillOptionsAsync(VeterinarianFormViewModel model)
        {
            model.SpecialtyOptions = await _specialtyService.GetAllAsync();
            model.ServiceOptions = await _serviceCatalog.GetAllAsync();
        }

        private static void FillAvailabilityList(AvailabilityViewModel model, Veterinarian veterinarian)
        {
            model.VeterinarianName = veterinarian.Title + " " + veterinarian.User.FullName;

            // DayOfWeek'te Pazar 0 olduğu için sona alınır; liste Pazartesi'den başlar, aynı gün içinde saate göre sıralanır.
            model.Availabilities = veterinarian.Availabilities
                .OrderBy(a => a.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .ToList();
        }
    }
}
