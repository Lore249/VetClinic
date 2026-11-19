using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Hizmet ekleme ve düzenleme formu. Id eklemede 0, düzenlemede kaydın Id'sidir.
    public class ServiceFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Hizmet adı en fazla {1} karakter olabilir.")]
        [Display(Name = "Hizmet adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla {1} karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Süre zorunludur.")]
        [Range(5, 480, ErrorMessage = "Süre {1} ile {2} dakika arasında olmalıdır.")]
        [Display(Name = "Süre (dakika)")]
        public int DurationMinutes { get; set; }

        // Sınırlar metin olarak verilir çünkü attribute parametresi decimal olamaz.
        [Required(ErrorMessage = "Ücret zorunludur.")]
        [Range(typeof(decimal), "1", "100000", ErrorMessage = "Ücret 1 ile 100.000 ₺ arasında olmalıdır.")]
        [Display(Name = "Ücret (₺)")]
        public decimal Price { get; set; }

        [Display(Name = "Aktif (yeni randevularda seçilebilir)")]
        public bool IsActive { get; set; } = true;
    }
}
