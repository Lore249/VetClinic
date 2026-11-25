using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Dtos
{
    // POST /api/auth/token isteğinin gövdesi. Alan eksikse [ApiController] otomatik olarak 400 döner.
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } = string.Empty;
    }
}
