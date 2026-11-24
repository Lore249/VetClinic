using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Admin randevu düzenleme formu. Yalnızca hekim ve başlangıç saati değiştirilebilir;
    // hizmet, süre ve ücret formda yoktur, bu yüzden dışarıdan gönderilseler de bağlanmazlar.
    public class AppointmentEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Veteriner seçin.")]
        [Display(Name = "Veteriner")]
        public int? VeterinarianId { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi ve saati zorunludur.")]
        [Display(Name = "Başlangıç")]
        public DateTime? StartTime { get; set; }

        // Yalnızca görüntüleme için (hayvan, hizmet, süre); controller doldurur, formdan alınmaz.
        // Nullable: formda olmadığı için MVC onu zorunlu sayıp hata üretmesin.
        [BindNever]
        public Appointment? Appointment { get; set; }

        [BindNever]
        public List<Veterinarian> VeterinarianOptions { get; set; } = new List<Veterinarian>();
    }
}
