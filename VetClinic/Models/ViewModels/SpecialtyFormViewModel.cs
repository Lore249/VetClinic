using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Uzmanlık ekleme ve düzenleme formu. Id eklemede 0, düzenlemede kaydın Id'sidir.
    public class SpecialtyFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Uzmanlık adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Uzmanlık adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Uzmanlık adı")]
        public string Name { get; set; } = string.Empty;
    }
}
