using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Services;

namespace VetClinic.Controllers.Api
{
    // Hekim listesi ve uygun saatler herkese açıktır (giriş gerekmez).
    [ApiController]
    [Route("api/veterinarians")]
    [AllowAnonymous]
    public class VeterinariansApiController : ControllerBase
    {
        private readonly IVeterinarianService _veterinarianService;
        private readonly IAppointmentService _appointmentService;

        public VeterinariansApiController(IVeterinarianService veterinarianService, IAppointmentService appointmentService)
        {
            _veterinarianService = veterinarianService;
            _appointmentService = appointmentService;
        }

        // GET /api/veterinarians              tüm hekimler
        // GET /api/veterinarians?specialtyId=2 yalnızca o uzmanlığa sahip hekimler (olmayan uzmanlık: 404)
        [HttpGet]
        public async Task<ActionResult<List<VeterinarianDto>>> GetAll([FromQuery] int? specialtyId)
        {
            var result = await _veterinarianService.GetPublicListAsync(specialtyId);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            return Ok(result.Data);
        }

        // GET /api/veterinarians/available?serviceId=1&date=2026-11-30
        // O gün o hizmeti verebilen ve boş saati olan hekimler. Parametre eksik ya da hatalıysa 400, hizmet yoksa 404.
        // Boş saatler randevu formundakiyle aynı servis metoduyla hesaplanır.
        [HttpGet("available")]
        public async Task<ActionResult<List<VeterinarianSlotsDto>>> GetAvailable(
            [FromQuery, Required(ErrorMessage = "serviceId zorunludur.")] int? serviceId,
            [FromQuery, Required(ErrorMessage = "date zorunludur (yyyy-MM-dd).")] DateOnly? date)
        {
            // [Required] sayesinde buraya gelindiyse ikisi de doludur.
            var result = await _appointmentService.GetAvailableSlotsAsync(serviceId!.Value, date!.Value);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            return Ok(result.Data);
        }
    }
}
