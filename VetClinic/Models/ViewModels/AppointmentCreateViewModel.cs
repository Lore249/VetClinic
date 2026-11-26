using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Randevu alma ekranı. 1. adımda hayvan, hizmet ve tarih seçilir; boş saatler REST API'den gelir. 2. adımda boş saatlerden
    // birine tıklanır ve hekim ile başlangıç saati, tıklanan düğmenin adresinden gelir (POST).
    // Bitiş saati, süre ve ücret formda yoktur: servis bunları hizmetten hesaplar.
    // Alanlar nullable: seçim kutuları "Seçin" ile boş açılır, boş gönderilirse [Required] mesajı çıkar.
    public class AppointmentCreateViewModel
    {
        [Required(ErrorMessage = "Hayvan seçin.")]
        [Display(Name = "Hayvan")]
        public int? PetId { get; set; }

        [Required(ErrorMessage = "Hizmet seçin.")]
        [Display(Name = "Hizmet")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Tarih seçin.")]
        [Display(Name = "Tarih")]
        public DateOnly? Date { get; set; }

        // Bu ikisi tıklanan saat düğmesinden gelir; normal kullanımda hiçbir zaman boş olmaz.
        [Required(ErrorMessage = "Hekim seçilmedi; boş saatlerden birine tıklayın.")]
        public int? VeterinarianId { get; set; }

        [Required(ErrorMessage = "Saat seçilmedi; boş saatlerden birine tıklayın.")]
        public DateTime? StartTime { get; set; }

        // Yalnızca görüntüleme için; controller doldurur, formdan alınmaz.
        [BindNever]
        public List<Pet> PetOptions { get; set; } = new List<Pet>();

        [BindNever]
        public List<Service> ServiceOptions { get; set; } = new List<Service>();
    }
}
