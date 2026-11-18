using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    // Hekimin uzmanlık alanı (dermatoloji, dahiliye vb.).
    public class Specialty
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Veterinarian> Veterinarians { get; set; } = new List<Veterinarian>();
    }
}
