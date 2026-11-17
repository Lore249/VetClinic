using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetClinic.Models.Entities;

namespace VetClinic.Data
{
    // IdentityDbContext, kullanıcı ve rol tablolarını (AspNetUsers, AspNetRoles, AspNetUserRoles...) hazır getirir.
    // Kliniğe ait tablolar Gün 2'de bu sınıfa eklenecek.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
