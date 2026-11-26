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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
