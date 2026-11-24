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

        // Kullanıcının görebildiği randevular (hayvan, sahip, hizmet ve hekim bilgisiyle):
        // Üye kendi hayvanlarınınkini, Veteriner kendisininkini, Admin hepsini görür.
        Task<List<Appointment>> GetAppointmentsAsync(string userId, string userRole);

        // Tek bir randevu; kullanıcı bu randevuyu göremiyorsa null döner.
        Task<Appointment?> GetAppointmentAsync(int appointmentId, string userId, string userRole);

        // Randevu şu anda iptal edilebilir / tamamlanabilir mi? Detay sayfaları düğmeyi göstermek için sorar;
        // CancelAsync ve CompleteAsync de aynı kuralları uygular, kurallar iki yerde yazılmaz.
        bool CanCancel(Appointment appointment);

        bool CanComplete(Appointment appointment);

        // Durum değiştiren işlemler. Hepsi işlemi yapan kullanıcıyı ve rolünü alır, yetkiyi kendisi kontrol eder:
        // göremediği randevu için NotFound, görebildiği ama yetkisi olmadığı işlem için Forbidden döner.

        // Bekliyor -> Onaylandı: Admin ya da randevunun hekimi.
        Task<ServiceResult<bool>> ApproveAsync(int appointmentId, string userId, string userRole);

        // Bekliyor -> Reddedildi: Admin ya da randevunun hekimi; ret nedeni zorunlu.
        Task<ServiceResult<bool>> RejectAsync(int appointmentId, string userId, string userRole, string? reason);

        // Onaylandı -> Tamamlandı: yalnızca randevunun hekimi, başlangıç saati geldikten sonra; muayene notu zorunlu.
        Task<ServiceResult<bool>> CompleteAsync(int appointmentId, string userId, string userRole, string? examinationNote);

        // Bekliyor ya da Onaylandı -> İptal Edildi: hayvanın sahibi ya da Admin; başlangıç saati geçmemiş olmalı.
        Task<ServiceResult<bool>> CancelAsync(int appointmentId, string userId, string userRole);

        // Yalnızca Admin: Bekliyor ya da Onaylandı randevunun hekimini ve başlangıç saatini değiştirir.
        // Hizmet, süre ve ücret değişmez; CreateAsync'in 2-7 numaralı kontrolleri uygulanır.
        Task<ServiceResult<bool>> UpdateAsync(int appointmentId, string userId, string userRole, int veterinarianId, DateTime startTime);

        // Yalnızca Admin.
        Task<ServiceResult<bool>> DeleteAsync(int appointmentId, string userId, string userRole);
    }
}
