using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Helpers;
using VetClinic.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SQL Server LocalDB; bağlantı dizesi appsettings.json'da durur ve parola içermez (Windows kimlik doğrulaması).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hazır Identity arayüzü yerine kendi AccountController'ımızı kullandığımız için AddDefaultIdentity değil AddIdentity.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Ödevin istediği "sau" şifresi kabul edilsin diye parola kuralları gevşetildi.
        options.Password.RequiredLength = 3;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>();

// Giriş yapmamış ya da yetkisi olmayan kullanıcının yönlendirileceği sayfalar (AccountController).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Açılışta bekleyen migration'lar uygulanır, ardından başlangıç verileri eklenir.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Kültür bilgisayarın ayarına bırakılmaz: sayı, tarih ve para biçimleri her makinede tr-TR olur.
var turkishCulture = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkishCulture),
    SupportedCultures = new List<CultureInfo> { turkishCulture },
    SupportedUICultures = new List<CultureInfo> { turkishCulture }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
