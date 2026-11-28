using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VetClinic.Models;
using VetClinic.Models.ViewModels;
using VetClinic.Services;

namespace VetClinic.Controllers
{
    public class HomeController : Controller
    {
        private readonly IServiceCatalogService _serviceCatalog;
        private readonly IClinicService _clinicService;

        public HomeController(IServiceCatalogService serviceCatalog, IClinicService clinicService)
        {
            _serviceCatalog = serviceCatalog;
            _clinicService = clinicService;
        }

        // Ana sayfa herkese açıktır. Hizmetler ve çalışma saatleri admin panelinde değiştirildiğinde burada da değişir.
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                Clinic = await _clinicService.GetClinicAsync(),
                Services = await _serviceCatalog.GetActiveAsync(),
                WorkingHours = await _clinicService.GetWorkingHoursAsync()
            };

            return View(model);
        }

        // Program.cs'teki UseStatusCodePagesWithReExecute, gövdesiz hata cevaplarında (404 gibi) isteği buraya yeniden çalıştırır.
        // Durum kodu değişmez (404 yine 404'tür); kullanıcıya yalnızca boş sayfa yerine Türkçe bir açıklama gösterilir.
        public IActionResult StatusPage(int code)
        {
            return View(code);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
