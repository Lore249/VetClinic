using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Evcil hayvan ekleme ve düzenleme formu. Sahip (OwnerId) burada yoktur: her zaman oturumdaki kullanıcıdan alınır.
    public class PetFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(50, ErrorMessage = "Ad en fazla {1} karakter olabilir.")]
        [Display(Name = "Ad")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tür zorunludur.")]
        [Display(Name = "Tür")]
        public PetSpecies Species { get; set; }

        [Required(ErrorMessage = "Irk zorunludur.")]
        [StringLength(50, ErrorMessage = "Irk en fazla {1} karakter olabilir.")]
        [Display(Name = "Irk")]
        public string Breed { get; set; } = string.Empty;

        // Nullable: yeni kayıt formunda kutu boş açılsın (yoksa 01.01.0001 görünürdü). Boş gönderilmesini [Required] engeller.
        [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
        [Display(Name = "Doğum tarihi")]
        public DateOnly? BirthDate { get; set; }

        // Nullable: kutu 0,00 yerine boş açılsın. Sınırlar ondalıklı olduğu için kültürden bağımsız (noktayla) yazıldı;
        // aynı değerler istemci tarafına da data-val-range-min/max olarak bu biçimde gider.
        [Required(ErrorMessage = "Kilo zorunludur.")]
        [Range(typeof(decimal), "0.01", "200", ParseLimitsInInvariantCulture = true, ErrorMessage = "Kilo 0,01 ile 200 kg arasında olmalıdır.")]
        [Display(Name = "Kilo (kg)")]
        public decimal? WeightKg { get; set; }

        // İsteğe bağlı fotoğraf. Uzantı ve boyut kuralları serviste kontrol edilir.
        [Display(Name = "Fotoğraf (isteğe bağlı, .jpg/.png, en fazla 2 MB)")]
        public IFormFile? Photo { get; set; }

        [Display(Name = "Mevcut fotoğrafı kaldır")]
        public bool RemovePhoto { get; set; }

        // Düzenleme sayfasında mevcut fotoğrafı göstermek için; controller doldurur, formdan alınmaz.
        [BindNever]
        public string? ExistingPhotoPath { get; set; }
    }
}
