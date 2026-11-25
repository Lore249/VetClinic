using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Services;

namespace VetClinic.Controllers.Api
{
    // Yalnızca Admin. Giriş yoksa 401, giriş var ama rol Admin değilse 403 döner.
    // Admin panelindeki rapor sayfası bu ucu tarayıcıdaki oturumla (cookie) jQuery AJAX ile çağırır.
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = AppRoles.Admin)]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsApiController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET /api/reports/services?from=2026-11-01&to=2026-11-30   (iki tarih de isteğe bağlı ve dahil)
        [HttpGet("services")]
        public async Task<ActionResult<List<ServiceReportDto>>> GetServiceReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var result = await _reportService.GetServiceReportAsync(from, to);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            return Ok(result.Data);
        }
    }
}
