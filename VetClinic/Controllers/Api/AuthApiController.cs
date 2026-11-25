using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Models.Dtos;
using VetClinic.Services;

namespace VetClinic.Controllers.Api
{
    // [ApiController]: gövde JSON'dan okunur, alanlar geçersizse action'a girmeden 400 döner, attribute route zorunludur.
    // ControllerBase: view desteği olmayan, yalnızca veri döndüren controller.
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthApiController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        // POST /api/auth/token   gövde: { "email": "...", "password": "..." }
        // Doğruysa 200 ve token; yanlışsa 401.
        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> CreateToken(LoginRequestDto request)
        {
            var token = await _tokenService.CreateTokenAsync(request.Email, request.Password);
            if (token == null)
            {
                return Problem(detail: "E-posta veya şifre hatalı.", statusCode: StatusCodes.Status401Unauthorized);
            }

            return Ok(token);
        }
    }
}
