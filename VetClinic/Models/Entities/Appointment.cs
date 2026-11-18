using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    public class Appointment
    {
        public int Id { get; set; }

        public int PetId { get; set; }

        public Pet Pet { get; set; } = null!;

        public int VeterinarianId { get; set; }

        public Veterinarian Veterinarian { get; set; } = null!;

        public int ServiceId { get; set; }

        public Service Service { get; set; } = null!;

        public DateTime StartTime { get; set; }

        // Başlangıç + süre. Çakışma sorgusu SQL'de çalışabilsin diye hesaplanıp saklanır.
        public DateTime EndTime { get; set; }

        // Randevu anındaki hizmet süresi ve ücretinin kopyası: hizmet sonradan değişse de
        // eski randevu ve ciro raporu değişmez.
        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // Hekimin tamamlanan randevuya girdiği not.
        [MaxLength(1000)]
        public string? ExaminationNote { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
