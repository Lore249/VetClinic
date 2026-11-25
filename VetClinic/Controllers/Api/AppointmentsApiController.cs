using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Helpers;
using VetClinic.Models.Dtos;
using VetClinic.Services;

namespace VetClinic.Controllers.Api
{
    // Randevu oluşturma ve görüntüleme. Kurallar MVC formuyla aynı servis metodunda (CreateAsync); burada yalnızca
    // sonuç HTTP durum koduna çevrilir: kural ihlali 400, bulunamayan kayıt 404, zaman çakışması 409.
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsApiController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // Kullanıcı id'si token'daki (ya da cookie'deki) kimlikten okunur; gövdeden alınmaz.
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST /api/appointments   gövde: { "petId": 1, "serviceId": 1, "veterinarianId": 1, "startTime": "2026-11-30T10:00" }
        // Başarılıysa 201 Created, Location: /api/appointments/{id} ve oluşan randevu.
        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequestDto request)
        {
            // [Required] alanlar eksikse [ApiController] buraya gelmeden 400 döner; bu yüzden değerler doludur.
            var result = await _appointmentService.CreateAsync(CurrentUserId, request.PetId!.Value, request.ServiceId!.Value,
                request.VeterinarianId!.Value, request.StartTime!.Value);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            var created = await _appointmentService.GetAppointmentDtoAsync(result.Data, CurrentUserId, AppRoles.Member);

            // CreatedAtAction: 201 durum kodu ve yeni kaydın adresi (GetById action'ının adresi) Location başlığına yazılır.
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, created.Data);
        }

        // GET /api/appointments/5
        // Yalnızca hayvanın sahibi görebilir: rol olarak Üye verilir, başkasının randevusu 404 döner.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentDto>> GetById(int id)
        {
            var result = await _appointmentService.GetAppointmentDtoAsync(id, CurrentUserId, AppRoles.Member);
            if (!result.Success)
            {
                return this.ToApiError(result);
            }

            return Ok(result.Data);
        }
    }
}
