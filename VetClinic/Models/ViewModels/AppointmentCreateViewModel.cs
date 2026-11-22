using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Randevu oluşturma formu. Bitiş saati, süre ve ücret formda yoktur: servis bunları hizmetten hesaplar.
    // Alanlar nullable: seçim kutuları "Seçin" ile boş açılır, boş gönderilirse [Required] mesajı çıkar.
    public class AppointmentCreateViewModel
    {
        [Required(ErrorMessage = "Hayvan seçin.")]
        [Display(Name = "Hayvan")]
        public int? PetId { get; set; }

        [Required(ErrorMessage = "Hizmet seçin.")]
        [Display(Name = "Hizmet")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Veteriner seçin.")]
        [Display(Name = "Veteriner")]
        public int? VeterinarianId { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi ve saati zorunludur.")]
        [Display(Name = "Başlangıç")]
        public DateTime? StartTime { get; set; }

        // Seçim kutularının seçenekleri; controller doldurur, formdan alınmaz.
        [BindNever]
        public List<Pet> PetOptions { get; set; } = new List<Pet>();

        [BindNever]
        public List<Service> ServiceOptions { get; set; } = new List<Service>();

        [BindNever]
        public List<Veterinarian> VeterinarianOptions { get; set; } = new List<Veterinarian>();
    }
}
