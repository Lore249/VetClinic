using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VetClinic.Models.ViewModels
{
    // Aşı kaydı ekleme ve düzenleme formu. PetId eklemede hangi hayvana ekleneceğini belirtir;
    // serviste bu hayvanın oturumdaki kullanıcıya ait olup olmadığı ayrıca kontrol edilir.
    public class VaccinationFormViewModel
    {
        public int Id { get; set; }

        public int PetId { get; set; }

        [Required(ErrorMessage = "Aşı adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Aşı adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Aşı adı")]
        public string VaccineName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yapıldığı tarih zorunludur.")]
        [Display(Name = "Yapıldığı tarih")]
        public DateOnly AdministeredOn { get; set; }

        // Tek dozluk aşılarda boş bırakılabilir.
        [Display(Name = "Sonraki doz tarihi (isteğe bağlı)")]
        public DateOnly? NextDoseOn { get; set; }

        // Sayfa başlığında hayvanın adını göstermek için; controller doldurur, formdan alınmaz.
        [BindNever]
        public string? PetName { get; set; }
    }
}
