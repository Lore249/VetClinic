using VetClinic.Models.Entities;

namespace VetClinic.Helpers
{
    // Enum'lar kodda İngilizce, veritabanında sayı olarak durur. Arayüz, API ve MCP aynı Türkçe metni
    // göstersin diye çeviri tek yerde yapılır (reflection ya da Display attribute okumadan).
    public static class EnumTexts
    {
        public static string ToTurkish(PetSpecies species)
        {
            return species switch
            {
                PetSpecies.Cat => "Kedi",
                PetSpecies.Dog => "Köpek",
                PetSpecies.Bird => "Kuş",
                PetSpecies.Rabbit => "Tavşan",
                PetSpecies.Other => "Diğer",
                _ => species.ToString()
            };
        }

        public static string ToTurkish(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Pending => "Bekliyor",
                AppointmentStatus.Approved => "Onaylandı",
                AppointmentStatus.Rejected => "Reddedildi",
                AppointmentStatus.Completed => "Tamamlandı",
                AppointmentStatus.Cancelled => "İptal Edildi",
                _ => status.ToString()
            };
        }

        public static string ToTurkish(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                DayOfWeek.Saturday => "Cumartesi",
                DayOfWeek.Sunday => "Pazar",
                _ => day.ToString()
            };
        }
    }
}
