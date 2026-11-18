namespace VetClinic.Models.Entities
{
    // Haftanın her günü için tek satır (DayOfWeek benzersiz indeksli).
    public class ClinicWorkingHour
    {
        public int Id { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        // Kapalı günlerde açılış ve kapanış saatleri dikkate alınmaz.
        public bool IsClosed { get; set; }
    }
}
