using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Üyenin randevu detay sayfası. Form değildir; yalnızca gösterilir.
    public class AppointmentDetailsViewModel
    {
        public Appointment Appointment { get; set; } = null!;

        // "İptal Et" düğmesi gösterilsin mi? Kararı servis verir (IAppointmentService.CanCancel); kural view'da tekrar yazılmaz.
        public bool CanCancel { get; set; }
    }
}
