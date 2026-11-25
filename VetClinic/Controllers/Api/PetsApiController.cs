using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Services;

namespace VetClinic.Controllers.Api
{
    // Giriş gerekir: JWT (Authorization: Bearer ...) ya da tarayıcıdaki oturum. Oturum yoksa 401 döner (302 değil).
    [ApiController]
    [Route("api/pets")]
    [Authorize]
    public class PetsApiController : ControllerBase
    {
        private readonly IVaccinationService _vaccinationService;

        public PetsApiController(IVaccinationService vaccinationService)
        {
            _vaccinationService = vaccinationService;
        }

        // Kullanıcı id'si token'daki (ya da cookie'deki) kimlikten okunur; adresten ya da gövdeden alınmaz.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET /api/pets/3/vaccinations
        // Hayvanın geçmiş, yaklaşan ve gecikmiş aşıları. Hayvan başkasınınsa 404 (varlığı sızmaz).
        [HttpGet("{id:int}/vaccinations")]
        public async Task<ActionResult<VaccinationScheduleDto>> GetVaccinations(int id)
        {
            var result = await _vaccinationService.GetScheduleAsync(CurrentUserId, id);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            return Ok(result.Data);
        }
    }
}
