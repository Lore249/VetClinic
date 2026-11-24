using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Randevuyu tamamlama formu (yalnızca hekim paneli). Muayene notu zorunlu kuralını ayrıca servis (CompleteAsync) uygular.
    public class CompleteAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Muayene notu zorunludur.")]
        [StringLength(1000, ErrorMessage = "Muayene notu en fazla {1} karakter olabilir.")]
        [Display(Name = "Muayene notu")]
        public string ExaminationNote { get; set; } = string.Empty;
    }
}
