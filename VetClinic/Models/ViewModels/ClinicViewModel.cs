using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Admin panelindeki klinik bilgileri formu. Tek klinik olduğu için Id taşınmaz; servis tablodaki tek satırı günceller.
    public class ClinicViewModel
    {
        [Required(ErrorMessage = "Klinik adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Klinik adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Klinik adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres zorunludur.")]
        [StringLength(250, ErrorMessage = "Adres en fazla {1} karakter olabilir.")]
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [StringLength(20, ErrorMessage = "Telefon en fazla {1} karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;
    }
}
