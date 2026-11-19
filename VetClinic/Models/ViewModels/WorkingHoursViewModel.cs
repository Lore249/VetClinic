using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.ViewModels
{
    // Haftanın yedi günü tek formda düzenlenir. Form alanları Days[0].OpenTime, Days[1].OpenTime... adlarıyla gelir
    // ve model binding bunları bu listeye doldurur.
    public class WorkingHoursViewModel
    {
        public List<WorkingHourRowViewModel> Days { get; set; } = new List<WorkingHourRowViewModel>();
    }

    // Tablodaki bir satır (bir gün). Yalnızca bu formda kullanıldığı için aynı dosyada duruyor.
    public class WorkingHourRowViewModel
    {
        // Gizli alanda taşınır; hangi günün güncelleneceğini belirler.
        public DayOfWeek DayOfWeek { get; set; }

        [Display(Name = "Kapalı")]
        public bool IsClosed { get; set; }

        // Değer tipleri için zorunluluk mesajı açıkça yazılmazsa istemci tarafında İngilizce varsayılan mesaj çıkar.
        [Required(ErrorMessage = "Açılış saati zorunludur.")]
        [Display(Name = "Açılış")]
        public TimeOnly OpenTime { get; set; }

        [Required(ErrorMessage = "Kapanış saati zorunludur.")]
        [Display(Name = "Kapanış")]
        public TimeOnly CloseTime { get; set; }
    }
}
