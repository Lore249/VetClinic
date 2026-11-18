namespace VetClinic.Models.Entities
{
    // Veritabanında sayı olarak saklanır. Sıra değiştirilse bile kayıtların anlamı bozulmasın diye değerler açıkça yazıldı.
    public enum AppointmentStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Completed = 3,
        Cancelled = 4
    }
}
