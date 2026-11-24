using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Randevu reddetme formu (hekim paneli ve admin paneli). [Required] kullanıcıyı göndermeden önce uyarır;
    // "ret nedeni zorunlu" kuralını ayrıca servis (RejectAsync) uygular.
    public class RejectAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Ret nedeni zorunludur.")]
        [StringLength(500, ErrorMessage = "Ret nedeni en fazla {1} karakter olabilir.")]
        [Display(Name = "Ret nedeni")]
        public string Reason { get; set; } = string.Empty;
    }
}
