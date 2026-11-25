namespace VetClinic.Models.Dtos
{
    // GET /api/pets/{id}/vaccinations cevabı. Hangi kaydın hangi listeye gireceğine VaccinationService karar verir;
    // REST API ve MCP aracı aynı sonucu kullanır.
    public class VaccinationScheduleDto
    {
        public int PetId { get; set; }

        public string PetName { get; set; } = string.Empty;

        // Yapılmış aşılar, en yeniden eskiye.
        public List<VaccinationDto> Past { get; set; } = new List<VaccinationDto>();

        // Sonraki dozu bugün ya da daha sonra olanlar, en yakından uzağa.
        public List<VaccinationDto> Upcoming { get; set; } = new List<VaccinationDto>();

        // Sonraki doz tarihi geçmiş olanlar (gecikmiş dozlar).
        public List<VaccinationDto> Overdue { get; set; } = new List<VaccinationDto>();
    }
}
