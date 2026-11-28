using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetClinic.Data;
using VetClinic.Models.Entities;
using VetClinic.Models.ViewModels;

namespace VetClinic.Services
{
    public class PetService : IPetService
    {
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxPhotoBytes = 2 * 1024 * 1024; // 2 MB

        // Ad karşılaştırması Türkçe kurallarla yapılır: "I"nın küçüğü "ı", "i"nin büyüğü "İ"dir.
        private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PetService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<Pet>> GetMyPetsAsync(string ownerId)
        {
            return await _context.Pets
                .AsNoTracking()
                .Where(p => p.OwnerId == ownerId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Pet?> GetMyPetAsync(string ownerId, int petId)
        {
            // Sahiplik her sorguda filtrelenir: başkasının hayvanı "yok" gibi görünür (null döner),
            // böylece adres çubuğunda id değiştiren üye o kaydın var olduğunu bile öğrenemez.
            return await _context.Pets
                .AsNoTracking()
                .Include(p => p.VaccinationRecords)
                .FirstOrDefaultAsync(p => p.Id == petId && p.OwnerId == ownerId);
        }

        public async Task<Pet?> FindMyPetByNameAsync(string ownerId, string name)
        {
            // Karşılaştırma SQL'de yapılmaz: SQL Server'ın harmanlaması Türkçe I/ı kuralını bilmez.
            // Kullanıcının hayvanları (birkaç kayıt) belleğe alınır ve ad C#'ta Türkçe kurallarla karşılaştırılır.
            var pets = await GetMyPetsAsync(ownerId);
            return pets.FirstOrDefault(p => IsSameNameInTurkish(p.Name, name.Trim()));
        }

        public async Task<ServiceResult<int>> CreateAsync(string ownerId, PetFormViewModel model)
        {
            // Yeni kayıtta karşılaştırmadan hariç tutulacak kayıt yok (0). Formdan gelen Id kullanılmaz.
            var error = await ValidateAsync(ownerId, model, 0);
            if (error != null)
            {
                return ServiceResult<int>.Validation(error);
            }

            // BirthDate ve WeightKg nullable; controller ModelState.IsValid ([Required]) kontrolünden sonra çağırdığı için doludur.
            var pet = new Pet
            {
                Name = model.Name.Trim(),
                Species = model.Species,
                Breed = model.Breed.Trim(),
                BirthDate = model.BirthDate!.Value,
                WeightKg = model.WeightKg!.Value,
                OwnerId = ownerId
            };

            if (model.Photo != null)
            {
                pet.PhotoPath = await SavePhotoAsync(model.Photo);
            }

            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(pet.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(string ownerId, PetFormViewModel model)
        {
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.Id == model.Id && p.OwnerId == ownerId);
            if (pet == null)
            {
                return ServiceResult<bool>.NotFound("Hayvan bulunamadı.");
            }

            var error = await ValidateAsync(ownerId, model, pet.Id);
            if (error != null)
            {
                return ServiceResult<bool>.Validation(error);
            }

            pet.Name = model.Name.Trim();
            pet.Species = model.Species;
            pet.Breed = model.Breed.Trim();
            pet.BirthDate = model.BirthDate!.Value;
            pet.WeightKg = model.WeightKg!.Value;

            // Yeni fotoğraf seçildiyse eskisinin yerini alır; seçilmediyse ve "kaldır" işaretliyse fotoğraf silinir.
            var oldPhotoPath = pet.PhotoPath;
            if (model.Photo != null)
            {
                pet.PhotoPath = await SavePhotoAsync(model.Photo);
            }
            else if (model.RemovePhoto)
            {
                pet.PhotoPath = null;
            }

            await _context.SaveChangesAsync();

            // Eski dosya, veritabanı kaydı başarıyla güncellendikten sonra silinir; kayıt başarısız olursa fotoğraf kaybolmaz.
            if (pet.PhotoPath != oldPhotoPath)
            {
                DeletePhotoFile(oldPhotoPath);
            }

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(string ownerId, int petId)
        {
            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.OwnerId == ownerId);
            if (pet == null)
            {
                return ServiceResult<bool>.NotFound("Hayvan bulunamadı.");
            }

            // İlişki Restrict olduğu için randevusu olan hayvan silinmeye çalışılırsa veritabanı hata verir; önce kontrol edilir.
            if (await _context.Appointments.AnyAsync(a => a.PetId == petId))
            {
                return ServiceResult<bool>.Validation("Bu hayvanın randevuları olduğu için silinemez. Geçmiş randevular ve muayene notları hayvan bilgisiyle birlikte saklanmalıdır.");
            }

            // Aşı kayıtları veritabanında Cascade ile hayvanla birlikte silinir.
            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();

            DeletePhotoFile(pet.PhotoPath);
            return ServiceResult<bool>.Ok(true);
        }

        // Ekleme ve güncellemede ortak kurallar (hepsi Validation türünde). Kurallara uyuyorsa null, değilse hata mesajı döner;
        // çağıran metot mesajı ServiceResult'a çevirir. excludedPetId: düzenlemede kaydın kendisi ad karşılaştırmasına girmesin.
        private async Task<string?> ValidateAsync(string ownerId, PetFormViewModel model, int excludedPetId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.BirthDate > today)
            {
                return "Doğum tarihi bugünden ileri olamaz.";
            }

            // Bir üyenin aynı adlı iki hayvanı olamaz. Karşılaştırma SQL'de değil bellekte yapılır (IsSameName'e bakın).
            var name = model.Name.Trim();
            var otherNames = await _context.Pets
                .Where(p => p.OwnerId == ownerId && p.Id != excludedPetId)
                .Select(p => p.Name)
                .ToListAsync();

            if (otherNames.Any(otherName => IsSameName(otherName, name)))
            {
                return "Bu adla kayıtlı başka bir hayvanınız var.";
            }

            return ValidatePhoto(model.Photo);
        }

        // İki ad, aşağıdaki kurallardan herhangi birine göre aynıysa aynı sayılır:
        // 1) Türkçe kural: MCP araçları hayvanı adıyla Türkçe kurallarla bulur, bu yüzden FISTIK = Fıstık olmalı.
        // 2) Veritabanının kuralı: (OwnerId, Name) benzersiz indeksi SQL Server'ın Latin harmanlamasıyla karşılaştırır
        //    ve IPEK = ipek sayar. Bu kontrol olmasa servis kabul eder, veritabanı reddeder ve kullanıcı hata sayfası görürdü.
        private static bool IsSameName(string first, string second)
        {
            return IsSameNameInTurkish(first, second)
                || string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        // Türkçe kurallarla, büyük/küçük harfe duyarsız karşılaştırma: FISTIK = fıstık, ama Fistik ≠ Fıstık.
        // Hem ad benzersizliği (IsSameName) hem MCP'deki hayvan araması (FindMyPetByNameAsync) bunu kullanır.
        private static bool IsSameNameInTurkish(string first, string second)
        {
            return string.Compare(first, second, TurkishCulture, CompareOptions.IgnoreCase) == 0;
        }

        private static string? ValidatePhoto(IFormFile? photo)
        {
            if (photo == null)
            {
                return null; // Fotoğraf isteğe bağlı.
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(extension))
            {
                return "Fotoğraf yalnızca .jpg, .jpeg veya .png uzantılı olabilir.";
            }

            if (photo.Length > MaxPhotoBytes)
            {
                return "Fotoğraf en fazla 2 MB olabilir.";
            }

            return null;
        }

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            // Kullanıcının verdiği dosya adı kullanılmaz: aynı adlı iki dosya birbirinin üzerine yazılmasın ve adda
            // "../" gibi klasör dışına çıkan parçalar olmasın diye ad Guid ile üretilir. Uzantı, kontrol edilmiş olandır.
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName).ToLowerInvariant();
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "pets");
            Directory.CreateDirectory(folder); // Klasör yoksa oluşturur, varsa bir şey yapmaz.

            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Veritabanına tarayıcının kullanacağı adres yazılır; UseStaticFiles bu adresi wwwroot altından sunar.
            return "/uploads/pets/" + fileName;
        }

        private void DeletePhotoFile(string? photoPath)
        {
            if (string.IsNullOrEmpty(photoPath))
            {
                return;
            }

            // Yoldan yalnızca dosya adı alınır; böylece uploads/pets klasörü dışındaki bir dosya hiçbir durumda silinemez.
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "pets", Path.GetFileName(photoPath));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
