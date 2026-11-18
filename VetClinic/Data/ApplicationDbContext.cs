using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetClinic.Models.Entities;

namespace VetClinic.Data
{
    // IdentityDbContext, kullanıcı ve rol tablolarını (AspNetUsers, AspNetRoles, AspNetUserRoles...) hazır getirir.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // null! : DbContext bu özellikleri kendisi doldurur.
        public DbSet<Clinic> Clinics { get; set; } = null!;
        public DbSet<ClinicWorkingHour> ClinicWorkingHours { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Specialty> Specialties { get; set; } = null!;
        public DbSet<Veterinarian> Veterinarians { get; set; } = null!;
        public DbSet<VeterinarianAvailability> VeterinarianAvailabilities { get; set; } = null!;
        public DbSet<Pet> Pets { get; set; } = null!;
        public DbSet<VaccinationRecord> VaccinationRecords { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Identity tablolarının ayarları temel sınıfta yapılır; bu satır olmazsa Identity tabloları bozulur.
            base.OnModelCreating(builder);

            // decimal için SQL tipi açıkça verilir; verilmezse EF uyarı verir ve küsurat kesilebilir.
            builder.Entity<Service>().Property(s => s.Price).HasPrecision(10, 2);
            builder.Entity<Appointment>().Property(a => a.Price).HasPrecision(10, 2);
            builder.Entity<Pet>().Property(p => p.WeightKg).HasPrecision(5, 2);

            // Haftanın her günü için yalnızca bir çalışma saati satırı olabilir.
            builder.Entity<ClinicWorkingHour>()
                .HasIndex(h => h.DayOfWeek)
                .IsUnique();

            // Bir kullanıcı hesabı en fazla bir veteriner kaydına bağlanır (bire bir; UserId benzersiz olur).
            builder.Entity<Veterinarian>()
                .HasOne(v => v.User)
                .WithOne()
                .HasForeignKey<Veterinarian>(v => v.UserId);

            // Çoka çok ilişkiler: ara tabloları EF kendisi oluşturur, adlarını okunur olsun diye biz veriyoruz.
            // Hekim ya da uzmanlık/hizmet silinince yalnızca ara tablodaki bağ satırları silinir.
            builder.Entity<Veterinarian>()
                .HasMany(v => v.Specialties)
                .WithMany(s => s.Veterinarians)
                .UsingEntity(j => j.ToTable("VeterinarianSpecialties"));

            builder.Entity<Veterinarian>()
                .HasMany(v => v.Services)
                .WithMany(s => s.Veterinarians)
                .UsingEntity(j => j.ToTable("VeterinarianServices"));

            // Hekim silinince müsaitlik aralıkları da silinir.
            builder.Entity<VeterinarianAvailability>()
                .HasOne(a => a.Veterinarian)
                .WithMany(v => v.Availabilities)
                .HasForeignKey(a => a.VeterinarianId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bir üyenin aynı adlı iki hayvanı olamaz; MCP araçları hayvanı adıyla bulur.
            builder.Entity<Pet>()
                .HasIndex(p => new { p.OwnerId, p.Name })
                .IsUnique();

            // Hayvan silinince aşı kayıtları da silinir.
            builder.Entity<VaccinationRecord>()
                .HasOne(v => v.Pet)
                .WithMany(p => p.VaccinationRecords)
                .HasForeignKey(v => v.PetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Randevusu olan hayvan, hekim ve hizmet silinemez (Restrict); geçmiş randevu kayıtları kaybolmasın.
            // WithMany() boş: Pet, Veterinarian ve Service tarafında randevu listesi özelliği tutmuyoruz.
            builder.Entity<Appointment>()
                .HasOne(a => a.Pet)
                .WithMany()
                .HasForeignKey(a => a.PetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Veterinarian)
                .WithMany()
                .HasForeignKey(a => a.VeterinarianId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
