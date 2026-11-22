namespace VetClinic.Models.Dtos
{
    // Bir hekimin belirli bir gündeki boş başlangıç saatleri. Entity değildir; randevu servisinin hesapladığı sonuçtur.
    // MVC randevu formu, REST API ve MCP aracı aynı sonucu kullanır.
    public class VeterinarianSlotsDto
    {
        public int VeterinarianId { get; set; }

        // Unvanıyla birlikte ad: "Vet. Hek. Selin Aydemir"
        public string VeterinarianName { get; set; } = string.Empty;

        public List<TimeOnly> StartTimes { get; set; } = new List<TimeOnly>();
    }
}
