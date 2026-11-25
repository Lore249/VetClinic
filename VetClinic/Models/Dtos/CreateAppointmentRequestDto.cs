using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Dtos
{
    // POST /api/appointments isteğinin gövdesi. Sahip (kullanıcı) burada yoktur; token'daki kimlikten alınır.
    // Bitiş, süre ve ücret de yoktur; servis bunları hizmetten hesaplar.
    // Alanlar nullable: gövdede hiç gönderilmezlerse 0 yerine null kalsınlar ve [Required] 400 döndürsün.
    public class CreateAppointmentRequestDto
    {
        [Required(ErrorMessage = "Hayvan (petId) zorunludur.")]
        public int? PetId { get; set; }

        [Required(ErrorMessage = "Hizmet (serviceId) zorunludur.")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Hekim (veterinarianId) zorunludur.")]
        public int? VeterinarianId { get; set; }

        // Yerel saat, örnek: "2026-11-30T10:00"
        [Required(ErrorMessage = "Başlangıç (startTime) zorunludur.")]
        public DateTime? StartTime { get; set; }
    }
}
