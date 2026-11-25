namespace VetClinic.Models.Dtos
{
    // Aşı takvimindeki bir kayıt.
    public class VaccinationDto
    {
        public int Id { get; set; }

        public string VaccineName { get; set; } = string.Empty;

        public DateOnly AdministeredOn { get; set; }

        public DateOnly? NextDoseOn { get; set; }
    }
}
