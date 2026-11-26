using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Ana sayfa: klinik bilgisi, aktif hizmetler ve haftalık çalışma saatleri (hepsi veritabanından). Form değildir.
    public class HomeViewModel
    {
        public Clinic? Clinic { get; set; }

        public List<Service> Services { get; set; } = new List<Service>();

        public List<ClinicWorkingHour> WorkingHours { get; set; } = new List<ClinicWorkingHour>();
    }
}
