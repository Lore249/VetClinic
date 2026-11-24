using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Randevu detay sayfaları (üye, hekim ve admin). Form değildir; yalnızca gösterilir.
    public class AppointmentDetailsViewModel
    {
        public Appointment Appointment { get; set; } = null!;

        // "İptal Et" ve "Tamamla" düğmeleri gösterilsin mi? Kararı servis verir (CanCancel, CanComplete);
        // zaman içeren bu kurallar view'da tekrar yazılmaz. Her panel yalnızca kendi rolünün düğmelerini gösterir.
        public bool CanCancel { get; set; }

        public bool CanComplete { get; set; }
    }
}
