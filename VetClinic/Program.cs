using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Helpers;
using VetClinic.Models.Entities;
using VetClinic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Model binding'in kendi ürettiği mesajlar (DataAnnotations dışındakiler) varsayılan olarak İngilizcedir.
    // Yalnızca formlarda karşılaşılabilecek olanlar Türkçeleştirildi.
    var messages = options.ModelBindingMessageProvider;
    // Sayı alanındaki data-val-number özniteliği; istemci tarafında "sayı girin" uyarısı olarak görünür.
    messages.SetValueMustBeANumberAccessor(field => $"{field} alanına geçerli bir sayı girin.");
    // Sayı ya da saat alanına dönüştürülemeyen bir değer gönderilirse (örnek: "abc").
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"'{value}' değeri {field} alanı için geçerli değil.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"{field} alanına girilen değer geçerli değil.");
    // Boş bırakılabilmesi mümkün olmayan (int, decimal, TimeOnly gibi) bir alan boş gönderilirse.
    messages.SetValueMustNotBeNullAccessor(value => "Bu alan zorunludur.");
});

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

// İş kuralları servis sınıflarında; controller'lar yalnızca bu arayüzleri kullanır.
// Scoped: her HTTP isteği için bir örnek oluşur, DbContext de istek başına olduğu için onunla aynı ömrü taşır.
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IVeterinarianService, VeterinarianService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IVaccinationService, VaccinationService>();

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

// MapStaticAssets yalnızca derleme anında wwwroot'ta bulunan dosyaları sunar. Üyelerin çalışırken yüklediği
// hayvan fotoğrafları (wwwroot/uploads/pets) sonradan oluştuğu için onları UseStaticFiles sunar.
// UseRouting'den sonra durur: css/js gibi bilinen dosyalar MapStaticAssets'le eşleşmiş olur ve UseStaticFiles onları atlar.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Admin paneli bir Area'dır: /Admin/Services/Edit/3 gibi adresler. /Admin yazılınca panelin ana sayfası açılır.
// Bu route varsayılan route'tan önce tanımlanır; {area:exists} yalnızca var olan bir area adıyla eşleşir.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
