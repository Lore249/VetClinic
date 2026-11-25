namespace VetClinic.Models.Dtos
{
    // GET /api/veterinarians cevabındaki bir hekim. Entity değil: hesap bilgileri (e-posta, şifre özeti vb.) dışarı çıkmaz.
    public class VeterinarianDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public List<IdNameDto> Specialties { get; set; } = new List<IdNameDto>();

        // Yalnızca aktif hizmetler; id, uygun saatler ucunda (serviceId) kullanılır.
        public List<IdNameDto> Services { get; set; } = new List<IdNameDto>();
    }
}
