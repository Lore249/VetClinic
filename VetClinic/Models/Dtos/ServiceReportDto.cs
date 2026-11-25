namespace VetClinic.Models.Dtos
{
    // GET /api/reports/services cevabındaki bir satır: bir hizmetin seçilen tarih aralığındaki randevu sayısı ve cirosu.
    public class ServiceReportDto
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        // Aralıktaki tüm randevular (her durumda).
        public int AppointmentCount { get; set; }

        public int CompletedCount { get; set; }

        // Yalnızca tamamlanan randevuların, randevu anında kopyalanan ücretlerinin toplamı.
        public decimal Revenue { get; set; }
    }
}
