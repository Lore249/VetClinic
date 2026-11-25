using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    // API'de sorgu dizesindeki bir parametre dönüştürülemezse (örnek: ?date=abc). Parametrelerin alan adı olmadığı için ayrı mesaj.
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"'{value}' değeri geçerli değil.");
})
.ConfigureApiBehaviorOptions(options =>
{
    // REST API hata cevaplarının (ProblemDetails) başlıkları Türkçe olsun; ayrıntı (detail) servisin mesajıdır.
    options.ClientErrorMapping[400].Title = "Geçersiz istek";
    options.ClientErrorMapping[401].Title = "Giriş gerekli";
    options.ClientErrorMapping[403].Title = "Bu işlem için yetkiniz yok";
    options.ClientErrorMapping[404].Title = "Bulunamadı";
    options.ClientErrorMapping[409].Title = "Çakışma";

    // [ApiController], istekteki alanlar geçersizse action'a hiç girmeden 400 döner. Varsayılan başlık İngilizce olduğu için
    // değiştirildi; alanların hataları DataAnnotations'taki Türkçe mesajlardır (ör. "Hizmet (serviceId) zorunludur.").
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Title = "İstekteki bazı alanlar geçersiz.",
            Status = StatusCodes.Status400BadRequest
        };
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    };
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

// /api ve /mcp ile başlayan istekler: bunlarda tarayıcı sayfasına yönlendirme yapılmaz, durum kodu döner.
static bool IsApiOrMcpRequest(HttpRequest request)
{
    return request.Path.StartsWithSegments("/api") || request.Path.StartsWithSegments("/mcp");
}

// Giriş yapmamış ya da yetkisi olmayan kullanıcının yönlendirileceği sayfalar (AccountController).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // API ve MCP isteklerinde giriş sayfasına 302 yönlendirmesi yerine oturum yoksa 401, rol yetmiyorsa 403 döner.
    // Bir API istemcisi HTML giriş sayfasıyla bir şey yapamaz; durum kodundan ne olduğunu anlar.
    options.Events.OnRedirectToLogin = context =>
    {
        if (IsApiOrMcpRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsApiOrMcpRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// JWT ayarları. Issuer, Audience ve süre appsettings.json'da; imza anahtarı ise depoya girmez, user-secrets'tan (Jwt:Key) okunur.
// Geliştirmede tanımlı değilse açılışta rastgele üretilir ki projeyi klonlayan kişi ek ayar yapmadan çalıştırabilsin.
// Bu durumda uygulama her açıldığında anahtar değişir ve önceki token'lar geçersiz olur.
if (string.IsNullOrEmpty(builder.Configuration["Jwt:Key"]))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Jwt:Key ayarı tanımlı değil. İmza anahtarını user-secrets ya da ortam değişkeniyle verin.");
    }

    builder.Configuration["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
var jwtKey = builder.Configuration["Jwt:Key"]!;

// Kimlik doğrulama şemaları: MVC sayfaları Identity cookie'siyle, REST API ve MCP JWT ile korunur.
// Varsayılan şema bir "policy scheme"dir: kendisi doğrulama yapmaz, isteğe bakıp hangi şemaya devredeceğini seçer.
const string JwtOrCookieScheme = "JwtOrCookie";
builder.Services.AddAuthentication(options =>
    {
        // AddIdentity varsayılanları cookie şemasına ayarlamıştı; burada seçici şemayla değiştirilir.
        options.DefaultScheme = JwtOrCookieScheme;
        options.DefaultAuthenticateScheme = JwtOrCookieScheme;
        options.DefaultChallengeScheme = JwtOrCookieScheme;
        options.DefaultForbidScheme = JwtOrCookieScheme;
    })
    .AddPolicyScheme(JwtOrCookieScheme, "JWT ya da cookie", options =>
    {
        // Authorization başlığı "Bearer " ile başlıyorsa JWT, değilse Identity cookie'si. Böylece API controller'ları hem token'ı
        // hem tarayıcıdaki oturumu kabul eder ve admin rapor sayfası aynı API'yi jQuery ile cookie'yle çağırabilir.
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }
            return IdentityConstants.ApplicationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        // Claim adları token'a yazıldığı gibi kalsın (ClaimTypes.NameIdentifier, ClaimTypes.Role); kısa adlara çevrilmesin.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Token'ı bu uygulama mı üretti, bu uygulama için mi üretildi, süresi geçmiş mi, imzası anahtarla tutuyor mu?
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

// İş kuralları servis sınıflarında; controller'lar yalnızca bu arayüzleri kullanır.
// Scoped: her HTTP isteği için bir örnek oluşur, DbContext de istek başına olduğu için onunla aynı ömrü taşır.
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IVeterinarianService, VeterinarianService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IVaccinationService, VaccinationService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IReportService, ReportService>();

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

// REST API controller'ları ([ApiController], Controllers/Api) adreslerini [Route] attribute'larından alır.
app.MapControllers();

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
