using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    // Üyenin kendi evcil hayvanları. Admin ve Veteriner bu sayfalara giremez.
    [Authorize(Roles = AppRoles.Member)]
    public class PetsController : Controller
    {
        private readonly IPetService _petService;

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        // Kullanıcı id'si hiçbir zaman formdan ya da adresten alınmaz; oturum çerezindeki kimlikten okunur.
        // [Authorize] girişi garanti ettiği için değer null olmaz (sondaki ! işareti derleyiciye bunu söyler).
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var pets = await _petService.GetMyPetsAsync(CurrentUserId);
            return View(pets);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Başkasının hayvanının id'si yazılırsa servis null döner ve 404 gösterilir.
            var pet = await _petService.GetMyPetAsync(CurrentUserId, id);
            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PetFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var error = await _petService.CreateAsync(CurrentUserId, model);
                if (error == null)
                {
                    TempData["SuccessMessage"] = "Hayvan kaydedildi.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pet = await _petService.GetMyPetAsync(CurrentUserId, id);
            if (pet == null)
            {
                return NotFound();
            }

            var model = new PetFormViewModel
            {
                Id = pet.Id,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                BirthDate = pet.BirthDate,
                WeightKg = pet.WeightKg,
                ExistingPhotoPath = pet.PhotoPath
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PetFormViewModel model)
        {
            // Formdaki gizli Id başkasının hayvanına çevrilmişse işlem yapılmadan 404 döner.
            var pet = await _petService.GetMyPetAsync(CurrentUserId, model.Id);
            if (pet == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var error = await _petService.UpdateAsync(CurrentUserId, model);
                if (error == null)
                {
                    TempData["SuccessMessage"] = "Hayvan bilgileri güncellendi.";
                    return RedirectToAction(nameof(Details), new { id = model.Id });
                }

                ModelState.AddModelError(string.Empty, error);
            }

            model.ExistingPhotoPath = pet.PhotoPath;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var pet = await _petService.GetMyPetAsync(CurrentUserId, id);
            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pet = await _petService.GetMyPetAsync(CurrentUserId, id);
            if (pet == null)
            {
                return NotFound();
            }

            var error = await _petService.DeleteAsync(CurrentUserId, id);
            if (error != null)
            {
                // Randevusu olan hayvan silinemez; onay sayfası hata mesajıyla yeniden gösterilir.
                ModelState.AddModelError(string.Empty, error);
                return View(pet);
            }

            TempData["SuccessMessage"] = "Hayvan ve aşı kayıtları silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
