using VetClinic.Models.Dtos;

namespace VetClinic.Services
{
    // Randevu iş kuralları yalnızca bu serviste yaşar. MVC formları, REST API ve MCP araçları aynı metotları çağırır;
    // böylece bir kural ihlal edildiğinde üç arayüz de aynı nedeni gösterir.
    public interface IAppointmentService
    {
        // O hizmeti verebilen hekimlerin o günkü boş başlangıç saatleri (15 dakikalık adımlarla).
        // Boş saati olmayan hekim listede yer almaz; klinik kapalıysa liste boştur.
        Task<ServiceResult<List<VeterinarianSlotsDto>>> GetAvailableSlotsAsync(int serviceId, DateOnly date);

        // Tüm kontrolleri sırayla yapar ve Bekliyor durumunda randevu oluşturur. Başarılıysa yeni randevunun id'sini döner.
        // ownerUserId her zaman oturumdaki kullanıcıdan gelir; hayvanın bu kullanıcıya ait olduğu ilk kontroldür.
        Task<ServiceResult<int>> CreateAsync(string ownerUserId, int petId, int serviceId, int veterinarianId, DateTime startTime);
    }
}
