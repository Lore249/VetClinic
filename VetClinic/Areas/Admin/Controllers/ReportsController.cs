using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;

namespace VetClinic.Areas.Admin.Controllers
{
    // Admin rapor sayfası. Bu action yalnızca sayfanın iskeletini (filtre formu ve boş tablo) döndürür;
    // veriler tarayıcıda jQuery AJAX ile REST API'den (GET /api/reports/services) alınır. Bkz. wwwroot/js/admin-report.js
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
