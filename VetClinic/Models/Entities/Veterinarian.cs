using System.ComponentModel.DataAnnotations;

namespace VetClinic.Models.Entities
{
    // Hekimin giriş hesabı ApplicationUser'dadır; adı da oradan (User.FullName) okunur, burada tekrar tutulmaz.
    public class Veterinarian
    {
        public int Id { get; set; }

        // Örnek: "Vet. Hek.", "Uzm. Vet. Hek."
        [Required]
        [MaxLength(50)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        // null! : EF bu özelliği veritabanından doldurur; derleyiciye "burada null kalmayacak" deniyor.
        public ApplicationUser User { get; set; } = null!;

        public ICollection<Specialty> Specialties { get; set; } = new List<Specialty>();

        public ICollection<Service> Services { get; set; } = new List<Service>();

        public ICollection<VeterinarianAvailability> Availabilities { get; set; } = new List<VeterinarianAvailability>();
    }
}
