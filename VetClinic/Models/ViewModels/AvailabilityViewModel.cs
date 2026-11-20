using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Hekimin müsaitlik sayfası: üstte yeni aralık ekleme formu, altta mevcut aralıkların listesi.
    public class AvailabilityViewModel
    {
        // Form alanları
        public int VeterinarianId { get; set; }

        [Required(ErrorMessage = "Gün zorunludur.")]
        [Display(Name = "Gün")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
        [Display(Name = "Başlangıç")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati zorunludur.")]
        [Display(Name = "Bitiş")]
        public TimeOnly EndTime { get; set; }

        // Yalnızca görüntüleme için; controller doldurur, formdan alınmaz.
        // string? : formda bu alan olmadığı için null olmasına izin verilir; yoksa MVC onu zorunlu sayıp hata üretirdi.
        [BindNever]
        public string? VeterinarianName { get; set; }

        [BindNever]
        public List<VeterinarianAvailability> Availabilities { get; set; } = new List<VeterinarianAvailability>();
    }
}
