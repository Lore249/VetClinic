using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Formdan yalnızca bu alanlar alınır; rol gibi alanlar burada olmadığı için dışarıdan gönderilemez (over-posting).
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla {1} karakter olabilir.")]
        [Display(Name = "Ad soyad")]
        public string FullName { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre (tekrar)")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
