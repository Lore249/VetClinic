using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VetClinic.Models.Entities
{
    // Identity'nin hazır kullanıcı tablosuna (AspNetUsers) ad soyad alanını eklemek için türetildi.
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }
}
