namespace VetClinic.Models.Dtos
{
    // API'nin döndürdüğü randevu. Entity yerine bu sınıf döner: ilişkili kayıtların yalnızca gereken alanları (ad gibi) taşınır,
    // sahip bilgisi ve iç alanlar dışarı çıkmaz.
    public class AppointmentDto
    {
        public int Id { get; set; }

        public int PetId { get; set; }

        public string PetName { get; set; } = string.Empty;

        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public int VeterinarianId { get; set; }

        public string VeterinarianName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        // Arayüzdeki ve MCP'deki metinle aynı: "Bekliyor", "Onaylandı"... (EnumTexts)
        public string Status { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public string? ExaminationNote { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
