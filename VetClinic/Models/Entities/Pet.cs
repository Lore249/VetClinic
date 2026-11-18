using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    public class Pet
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public PetSpecies Species { get; set; }

        [Required]
        [MaxLength(50)]
        public string Breed { get; set; } = string.Empty;

        public DateOnly BirthDate { get; set; }

        public decimal WeightKg { get; set; }

        // Fotoğraf isteğe bağlı; dosya wwwroot/uploads/pets altında durur, burada yalnızca yolu saklanır.
        [MaxLength(260)]
        public string? PhotoPath { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        public ApplicationUser Owner { get; set; } = null!;

        public ICollection<VaccinationRecord> VaccinationRecords { get; set; } = new List<VaccinationRecord>();
    }
}
