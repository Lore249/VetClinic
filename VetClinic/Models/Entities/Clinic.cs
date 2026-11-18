using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    // Ödev tek klinik kabul ettiği için bu tabloda yalnızca bir satır bulunur.
    public class Clinic
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
    }
}
