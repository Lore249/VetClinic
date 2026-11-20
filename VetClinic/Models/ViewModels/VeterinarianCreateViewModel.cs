using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Hekim ekleme formu: düzenleme formundaki alanlara ek olarak hekimin giriş hesabı için e-posta ve ilk şifre.
    public class VeterinarianCreateViewModel : VeterinarianFormViewModel
    {
        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [StringLength(256, ErrorMessage = "E-posta en fazla {1} karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Şifre en az {2}, en fazla {1} karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;
    }
}
