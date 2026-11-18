using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    public class VaccinationRecord
    {
        public int Id { get; set; }

        public int PetId { get; set; }

        public Pet Pet { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string VaccineName { get; set; } = string.Empty;

        public DateOnly AdministeredOn { get; set; }

        // Tek dozluk aşılarda sonraki doz olmayabilir.
        public DateOnly? NextDoseOn { get; set; }
    }
}
