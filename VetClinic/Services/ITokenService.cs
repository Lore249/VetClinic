using VetClinic.Models.Dtos;

namespace VetClinic.Services
{
    // REST API ve MCP istemcileri için JWT üretir.
    public interface ITokenService
    {
        // E-posta ve şifre doğruysa token üretir; yanlışsa null döner (controller 401 verir).
        // Hangisinin yanlış olduğu söylenmez; böylece hangi e-postaların kayıtlı olduğu dışarıdan anlaşılamaz.
        Task<TokenResponseDto?> CreateTokenAsync(string email, string password);
    }
}
