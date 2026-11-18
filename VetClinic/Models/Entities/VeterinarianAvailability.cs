namespace VetClinic.Models.Entities
{
    // Hekimin haftalık çalışma aralığı. Aynı günde birden fazla aralık olabilir (öğle arası gibi).
    public class VeterinarianAvailability
    {
        public int Id { get; set; }

        public int VeterinarianId { get; set; }

        public Veterinarian Veterinarian { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }
    }
}
