using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetClinic.Helpers;
using VetClinic.Models.Entities;

namespace VetClinic.Data
{
    // Uygulama her açıldığında çalışır. Her adım önce kaydın var olup olmadığına baktığı için kayıt çoğaltmaz.
    public static class DbSeeder
    {
        private const string AdminEmail = "b241210016@sakarya.edu.tr";

        // Ödevin istediği demo parolası; gerçek bir sır değildir.
        private const string DemoPassword = "sau";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Projeyi klonlayan kişi "dotnet ef database update" çalıştırmak zorunda kalmasın:
            // veritabanı yoksa oluşturulur, bekleyen migration'lar uygulanır.
            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);
            await SeedAdminAsync(userManager);
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

        private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
        {
            if (await userManager.FindByEmailAsync(AdminEmail) != null)
            {
                return;
            }

            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Kerem EROL"
            };

            var result = await userManager.CreateAsync(admin, DemoPassword);
            if (!result.Succeeded)
            {
                // Sessizce geçilseydi admin girişi neden yapılamıyor anlaşılmazdı; hata açılışta görünsün.
                throw new InvalidOperationException("Admin hesabı oluşturulamadı: "
                    + string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
