using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;

namespace VetClinic.Areas.Admin.Controllers
{
    // [Area] olmadan bu controller /Admin/... adreslerinden bulunamaz; [Authorize] ile yalnızca Admin girebilir.
    [Area("Admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public class DashboardController : Controller
    {
        // /Admin adresi buraya düşer (Program.cs'teki area route'unda varsayılan controller Dashboard).
        public IActionResult Index()
        {
            return View();
        }
    }
}
