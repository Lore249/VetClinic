using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Boş saatler sayfası: hizmet ve tarih seçilir (GET formu), hizmeti verebilen hekimlerin boş saatleri listelenir.
    public class AppointmentSlotsViewModel
    {
        [Display(Name = "Hizmet")]
        public int? ServiceId { get; set; }

        [Display(Name = "Tarih")]
        public DateOnly? Date { get; set; }

        // Yalnızca görüntüleme için; controller doldurur, formdan alınmaz.
        [BindNever]
        public List<Service> ServiceOptions { get; set; } = new List<Service>();

        // null: henüz arama yapılmadı. Boş liste: arandı ama uygun saat yok.
        [BindNever]
        public List<VeterinarianSlotsDto>? Slots { get; set; }
    }
}
