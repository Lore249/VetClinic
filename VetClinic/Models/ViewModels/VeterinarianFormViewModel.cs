using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VetClinic.Models.Entities;

namespace VetClinic.Models.ViewModels
{
    // Hekim düzenleme formu. Ekleme formu (VeterinarianCreateViewModel) bundan türeyip e-posta ve şifre ekler.
    // E-posta ve şifre giriş bilgisi olduğu için düzenlemede yer almaz; formdan gönderilse bile bağlanacak özellik yoktur.
    public class VeterinarianFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla {1} karakter olabilir.")]
        [Display(Name = "Ad soyad")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unvan zorunludur.")]
        [StringLength(50, ErrorMessage = "Unvan en fazla {1} karakter olabilir.")]
        [Display(Name = "Unvan")]
        public string Title { get; set; } = string.Empty;

        // İşaretlenen onay kutularının değerleri (id'ler) bu listelere dolar. Hiçbiri işaretlenmezse liste boş kalır.
        public List<int> SelectedSpecialtyIds { get; set; } = new List<int>();

        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        // Onay kutularını çizmek için tüm seçenekler. Controller doldurur; [BindNever] ile formdan hiçbir zaman alınmaz.
        [BindNever]
        public List<Specialty> SpecialtyOptions { get; set; } = new List<Specialty>();

        [BindNever]
        public List<Service> ServiceOptions { get; set; } = new List<Service>();
    }
}
