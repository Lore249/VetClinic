using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetClinic.Helpers;
using VetClinic.Models.Entities;

namespace VetClinic.Data
{
    // Uygulama her açıldığında çalışır. Kural: bir tablo boşsa örnek veri eklenir, doluysa dokunulmaz.
    // Bu yüzden seed tekrar çalıştığında kayıt çoğaltmaz.
    public static class DbSeeder
    {
        private const string AdminEmail = "b241210016@sakarya.edu.tr";
        private const string MemberEmail = "ece.kandemir@example.com";
        private const string SelinEmail = "selin.aydemir@example.com";
        private const string OkanEmail = "okan.yalcinkaya@example.com";
        private const string DefneEmail = "defne.arikan@example.com";

        // Ödevin istediği demo parolası; gerçek bir sır değildir. Tüm demo hesaplarında aynıdır.
        private const string DemoPassword = "sau";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Projeyi klonlayan kişi "dotnet ef database update" çalıştırmak zorunda kalmasın:
            // veritabanı yoksa oluşturulur, bekleyen migration'lar uygulanır.
            await context.Database.MigrateAsync();

            // Sıra önemli: hekimler hizmet ve uzmanlıklara, randevular hayvanlara ve hekimlere bağlı.
            await SeedRolesAsync(roleManager);
            await GetOrCreateUserAsync(userManager, AdminEmail, "Kerem EROL", AppRoles.Admin);
            await SeedClinicAsync(context);
            await SeedServicesAsync(context);
            await SeedSpecialtiesAsync(context);
            await SeedVeterinariansAsync(context, userManager);
            await SeedMemberWithPetsAsync(context, userManager);
            await SeedAppointmentsAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { AppRoles.Admin, AppRoles.Veterinarian, AppRoles.Member };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedClinicAsync(ApplicationDbContext context)
        {
            if (!await context.Clinics.AnyAsync())
            {
                context.Clinics.Add(new Clinic
                {
                    Name = "Kerem'in Kliniği",
                    Address = "Yeşilpati Sokak No: 7, Serdivan / Sakarya",
                    Phone = "(0264) 000 12 34"
                });
            }

            if (!await context.ClinicWorkingHours.AnyAsync())
            {
                // Pazartesi-Cumartesi 09.00-20.00, Pazar kapalı.
                DayOfWeek[] days =
                {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                    DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
                };

                foreach (var day in days)
                {
                    context.ClinicWorkingHours.Add(new ClinicWorkingHour
                    {
                        DayOfWeek = day,
                        OpenTime = new TimeOnly(9, 0),
                        CloseTime = new TimeOnly(20, 0),
                        IsClosed = day == DayOfWeek.Sunday
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedServicesAsync(ApplicationDbContext context)
        {
            if (await context.Services.AnyAsync())
            {
                return;
            }

            // m soneki: sayının decimal olduğunu belirtir (para tutarlarında küsurat hatası olmasın).
            context.Services.AddRange(
                new Service { Name = "Muayene", Description = "Genel sağlık kontrolü, ateş ve kilo ölçümü, hekim değerlendirmesi.", DurationMinutes = 30, Price = 1250m },
                new Service { Name = "Aşı", Description = "Aşı takvimine uygun aşı uygulaması ve kaydı.", DurationMinutes = 15, Price = 850m },
                new Service { Name = "Kısırlaştırma", Description = "Genel anestezi altında operasyon ve ilk kontrol.", DurationMinutes = 120, Price = 7500m },
                new Service { Name = "Diş Temizliği", Description = "Diş taşı temizliği ve ağız içi muayene.", DurationMinutes = 60, Price = 3000m },
                new Service { Name = "Tıraş ve Bakım", Description = "Tüy kesimi, tarama, tırnak kesimi ve kulak temizliği.", DurationMinutes = 45, Price = 1100m });

            await context.SaveChangesAsync();
        }

        private static async Task SeedSpecialtiesAsync(ApplicationDbContext context)
        {
            if (await context.Specialties.AnyAsync())
            {
                return;
            }

            context.Specialties.AddRange(
                new Specialty { Name = "Dermatoloji" },
                new Specialty { Name = "Dahiliye" },
                new Specialty { Name = "Etoloji" },
                new Specialty { Name = "Hayvan Refahı" });

            await context.SaveChangesAsync();
        }

        private static async Task SeedVeterinariansAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (await context.Veterinarians.AnyAsync())
            {
                return;
            }

            // Hekimlerin müsaitlikleri bilerek farklı seçildi; boş saat hesabı sunumda daha iyi görünsün.
            var selin = await CreateVeterinarianAsync(context, userManager, SelinEmail, "Selin Aydemir", "Vet. Hek.",
                new[] { "Dermatoloji", "Dahiliye" },
                new[] { "Muayene", "Aşı", "Tıraş ve Bakım" });

            DayOfWeek[] weekdays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            foreach (var day in weekdays)
            {
                // Öğle arası olduğu için aynı günde iki ayrı aralık.
                AddAvailability(selin, day, new TimeOnly(9, 0), new TimeOnly(12, 30));
                AddAvailability(selin, day, new TimeOnly(13, 30), new TimeOnly(17, 0));
            }

            var okan = await CreateVeterinarianAsync(context, userManager, OkanEmail, "Okan Yalçınkaya", "Uzm. Vet. Hek.",
                new[] { "Dahiliye", "Hayvan Refahı" },
                new[] { "Muayene", "Aşı", "Kısırlaştırma", "Diş Temizliği" });
            AddAvailability(okan, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(20, 0));
            AddAvailability(okan, DayOfWeek.Wednesday, new TimeOnly(12, 0), new TimeOnly(20, 0));
            AddAvailability(okan, DayOfWeek.Friday, new TimeOnly(12, 0), new TimeOnly(20, 0));
            AddAvailability(okan, DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(15, 0));

            var defne = await CreateVeterinarianAsync(context, userManager, DefneEmail, "Defne Arıkan", "Vet. Hek.",
                new[] { "Etoloji", "Hayvan Refahı" },
                new[] { "Muayene", "Aşı" });
            AddAvailability(defne, DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(19, 0));
            AddAvailability(defne, DayOfWeek.Thursday, new TimeOnly(10, 0), new TimeOnly(19, 0));
            AddAvailability(defne, DayOfWeek.Saturday, new TimeOnly(13, 0), new TimeOnly(20, 0));

            await context.SaveChangesAsync();
        }

        private static async Task SeedMemberWithPetsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (await context.Pets.AnyAsync())
            {
                return;
            }

            var member = await GetOrCreateUserAsync(userManager, MemberEmail, "Ece Kandemir", AppRoles.Member);

            // Aşı tarihleri seed'in çalıştığı güne göre hesaplanır: proje hangi gün açılırsa açılsın
            // yaklaşan aşı yaklaşan, gecikmiş aşı gecikmiş görünür.
            var today = DateOnly.FromDateTime(DateTime.Today);

            var fistik = new Pet { Name = "Fıstık", Species = PetSpecies.Cat, Breed = "Tekir", BirthDate = new DateOnly(2021, 4, 12), WeightKg = 4.2m, OwnerId = member.Id };
            fistik.VaccinationRecords.Add(new VaccinationRecord { VaccineName = "Karma aşı", AdministeredOn = today.AddMonths(-11), NextDoseOn = today.AddDays(20) }); // yaklaşan
            fistik.VaccinationRecords.Add(new VaccinationRecord { VaccineName = "Kuduz", AdministeredOn = today.AddMonths(-5), NextDoseOn = today.AddMonths(7) });

            var pasa = new Pet { Name = "Paşa", Species = PetSpecies.Dog, Breed = "Golden Retriever", BirthDate = new DateOnly(2019, 9, 3), WeightKg = 31.5m, OwnerId = member.Id };
            pasa.VaccinationRecords.Add(new VaccinationRecord { VaccineName = "Kuduz", AdministeredOn = today.AddMonths(-13), NextDoseOn = today.AddDays(-30) }); // gecikmiş
            pasa.VaccinationRecords.Add(new VaccinationRecord { VaccineName = "Karma aşı", AdministeredOn = today.AddMonths(-2), NextDoseOn = today.AddMonths(10) });

            var bulut = new Pet { Name = "Bulut", Species = PetSpecies.Rabbit, Breed = "Hollanda Lop", BirthDate = new DateOnly(2023, 2, 14), WeightKg = 1.8m, OwnerId = member.Id };
            bulut.VaccinationRecords.Add(new VaccinationRecord { VaccineName = "RHD aşısı", AdministeredOn = today.AddMonths(-3), NextDoseOn = today.AddMonths(9) });

            context.Pets.AddRange(fistik, pasa, bulut);
            await context.SaveChangesAsync();
        }

        private static async Task SeedAppointmentsAsync(ApplicationDbContext context)
        {
            if (await context.Appointments.AnyAsync())
            {
                return;
            }

            var pets = await context.Pets.Where(p => p.Owner.Email == MemberEmail).ToListAsync();
            var veterinarians = await context.Veterinarians.Include(v => v.User).ToListAsync();
            var services = await context.Services.ToListAsync();

            var fistik = pets.FirstOrDefault(p => p.Name == "Fıstık");
            var pasa = pets.FirstOrDefault(p => p.Name == "Paşa");
            var bulut = pets.FirstOrDefault(p => p.Name == "Bulut");
            var selin = veterinarians.FirstOrDefault(v => v.User.Email == SelinEmail);
            var okan = veterinarians.FirstOrDefault(v => v.User.Email == OkanEmail);
            var defne = veterinarians.FirstOrDefault(v => v.User.Email == DefneEmail);
            var checkup = services.FirstOrDefault(s => s.Name == "Muayene");
            var vaccine = services.FirstOrDefault(s => s.Name == "Aşı");
            var dental = services.FirstOrDefault(s => s.Name == "Diş Temizliği");

            // Örnek veriler sonradan değiştirilmişse (hekim silinmiş, hayvan adı değişmiş vb.) randevu eklenmez;
            // böylece seed hiçbir durumda uygulamanın açılmasını engellemez.
            if (fistik == null || pasa == null || bulut == null || selin == null || okan == null || defne == null
                || checkup == null || vaccine == null || dental == null)
            {
                return;
            }

            // Tarihler seed gününe göre hesaplanır ve hekimin çalıştığı güne, müsaitlik saatinin içine denk getirilir.
            var today = DateOnly.FromDateTime(DateTime.Today);

            context.Appointments.AddRange(
                // Geçmiş, tamamlanmış randevular: veteriner paneli ve ciro raporu boş görünmesin.
                CreateAppointment(pasa, okan, checkup, LastDateOnOrBefore(today.AddDays(-40), DayOfWeek.Monday), new TimeOnly(14, 0),
                    AppointmentStatus.Completed,
                    "Genel kontrol yapıldı. Kilo ideal aralıkta, kalp ve akciğer sesleri normal. Kulaklarda hafif kızarıklık görüldü; haftada iki kez kulak temizleme solüsyonu önerildi."),
                CreateAppointment(fistik, selin, checkup, LastDateOnOrBefore(today.AddDays(-21), DayOfWeek.Tuesday), new TimeOnly(10, 0),
                    AppointmentStatus.Completed,
                    "Boyun ve sırt bölgesinde tüy dökülmesi şikâyeti. Mantar ve parazit bulgusu yok; mama değişikliğine bağlı alerji düşünüldü. Hipoalerjenik mamaya geçilmesi önerildi."),
                CreateAppointment(pasa, okan, dental, LastDateOnOrBefore(today.AddDays(-7), DayOfWeek.Wednesday), new TimeOnly(15, 0),
                    AppointmentStatus.Completed,
                    "Genel anestezi altında diş taşları temizlendi. Sol üst azı dişinde hafif diş eti çekilmesi var; altı ay sonra kontrol önerildi."),

                // Gelecekteki randevular: biri onay bekliyor, biri onaylanmış.
                CreateAppointment(bulut, defne, checkup, FirstDateAfter(today, DayOfWeek.Tuesday, DayOfWeek.Thursday), new TimeOnly(11, 0),
                    AppointmentStatus.Pending, null),
                CreateAppointment(fistik, selin, vaccine, FirstDateAfter(today, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday), new TimeOnly(14, 0),
                    AppointmentStatus.Approved, null));

            await context.SaveChangesAsync();
        }

        // Hesap varsa onu döndürür; yoksa demo parolasıyla oluşturup role ekler.
        private static async Task<ApplicationUser> GetOrCreateUserAsync(UserManager<ApplicationUser> userManager, string email, string fullName, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return user;
            }

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                // Sessizce geçilseydi bu hesapla neden giriş yapılamadığı anlaşılmazdı; hata açılışta görünsün.
                throw new InvalidOperationException(email + " hesabı oluşturulamadı: "
                    + string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        private static async Task<Veterinarian> CreateVeterinarianAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            string email, string fullName, string title, string[] specialtyNames, string[] serviceNames)
        {
            var user = await GetOrCreateUserAsync(userManager, email, fullName, AppRoles.Veterinarian);

            var veterinarian = new Veterinarian
            {
                Title = title,
                UserId = user.Id,
                Specialties = await context.Specialties.Where(s => specialtyNames.Contains(s.Name)).ToListAsync(),
                Services = await context.Services.Where(s => serviceNames.Contains(s.Name)).ToListAsync()
            };

            context.Veterinarians.Add(veterinarian);
            return veterinarian;
        }

        private static void AddAvailability(Veterinarian veterinarian, DayOfWeek day, TimeOnly start, TimeOnly end)
        {
            veterinarian.Availabilities.Add(new VeterinarianAvailability
            {
                DayOfWeek = day,
                StartTime = start,
                EndTime = end
            });
        }

        // Seed randevuları CreateAsync'ten geçmez (geçmiş tarihli olanlar "gelecekte olmalı" kuralına takılırdı);
        // süre ve ücret yine de hizmetten kopyalanır.
        private static Appointment CreateAppointment(Pet pet, Veterinarian veterinarian, Service service,
            DateOnly date, TimeOnly time, AppointmentStatus status, string? examinationNote)
        {
            var start = date.ToDateTime(time);

            return new Appointment
            {
                PetId = pet.Id,
                VeterinarianId = veterinarian.Id,
                ServiceId = service.Id,
                StartTime = start,
                EndTime = start.AddMinutes(service.DurationMinutes),
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                Status = status,
                ExaminationNote = examinationNote,
                // Geçmiş randevular birkaç gün önceden alınmış görünsün; gelecektekiler bugün alınmış olsun.
                CreatedAt = start < DateTime.Now ? start.AddDays(-3) : DateTime.Now
            };
        }

        // Verilen tarihten geriye doğru giderek istenen haftanın gününe denk gelen ilk tarihi bulur.
        private static DateOnly LastDateOnOrBefore(DateOnly date, DayOfWeek day)
        {
            while (date.DayOfWeek != day)
            {
                date = date.AddDays(-1);
            }
            return date;
        }

        // Verilen tarihten sonraki, istenen günlerden birine denk gelen ilk tarihi bulur.
        // params: günler dizi yazmadan virgülle verilebilir.
        private static DateOnly FirstDateAfter(DateOnly date, params DayOfWeek[] days)
        {
            var result = date.AddDays(1);
            while (!days.Contains(result.DayOfWeek))
            {
                result = result.AddDays(1);
            }
            return result;
        }
    }
}
