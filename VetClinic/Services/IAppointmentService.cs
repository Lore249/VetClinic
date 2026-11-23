using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;

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

        // Üyenin hayvanlarına ait tüm randevular (hayvan, hizmet ve hekim bilgisiyle).
        Task<List<Appointment>> GetMyAppointmentsAsync(string ownerUserId);

        // Üyenin tek bir randevusu; randevu başkasının hayvanına aitse null döner.
        Task<Appointment?> GetMyAppointmentAsync(string ownerUserId, int appointmentId);

        // Randevu şu anda iptal edilebilir mi? Detay sayfası "İptal Et" düğmesini göstermek için sorar;
        // CancelAsync de aynı kuralı uygular, kural iki yerde yazılmaz.
        bool CanCancel(Appointment appointment);

        // Bekliyor ya da Onaylandı durumundaki, başlangıcı gelmemiş randevuyu iptal eder.
        // Yalnızca hayvanın sahibi (Üye) ya da Admin yapabilir; yetkiyi servis kendisi kontrol eder.
        Task<ServiceResult<bool>> CancelAsync(int appointmentId, string userId, string userRole);
    }
}
