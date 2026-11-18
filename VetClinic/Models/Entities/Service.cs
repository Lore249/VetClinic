using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    // Klinikte verilen hizmet (muayene, aşı vb.). Süre, randevunun bitiş saatini ve çakışma hesabını belirler.
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }

        // Randevusu olan hizmet silinemez; kullanımdan kalkınca pasif yapılır ve yeni randevuda seçilemez.
        public bool IsActive { get; set; } = true;

        public ICollection<Veterinarian> Veterinarians { get; set; } = new List<Veterinarian>();
    }
}
