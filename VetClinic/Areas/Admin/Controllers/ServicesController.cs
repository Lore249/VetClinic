using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Areas.Admin.Controllers
{
    // Hizmet CRUD. Controller yalnızca formu alır, servisi çağırır ve sonucu gösterir; kurallar ServiceCatalogService'te.
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class ServicesController : Controller
    {
        private readonly IServiceCatalogService _serviceCatalog;

        public ServicesController(IServiceCatalogService serviceCatalog)
        {
            _serviceCatalog = serviceCatalog;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _serviceCatalog.GetAllAsync();
            return View(services);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ServiceFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _serviceCatalog.CreateAsync(model);
            if (!result.Success)
            {
                return this.HandleServiceError(result) ?? View(model);
            }

            TempData["SuccessMessage"] = "Hizmet eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _serviceCatalog.GetByIdAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var model = new ServiceFormViewModel
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                IsActive = service.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _serviceCatalog.UpdateAsync(model);
            if (!result.Success)
            {
                return this.HandleServiceError(result) ?? View(model);
            }

            TempData["SuccessMessage"] = "Hizmet güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _serviceCatalog.GetByIdAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _serviceCatalog.DeleteAsync(id);
            if (!result.Success)
            {
                var errorResult = this.HandleServiceError(result);
                if (errorResult != null)
                {
                    return errorResult;
                }

                // Silinemediyse (örneğin randevusu varsa) onay sayfası hata mesajıyla yeniden gösterilir.
                var service = await _serviceCatalog.GetByIdAsync(id);
                if (service == null)
                {
                    return NotFound();
                }

                return View(service);
            }

            TempData["SuccessMessage"] = "Hizmet silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
