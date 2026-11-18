namespace VetClinic.Models.Entities
{
    // Veritabanında sayı olarak saklanır. Sıra değiştirilse bile kayıtların anlamı bozulmasın diye değerler açıkça yazıldı.
    public enum PetSpecies
    {
        Cat = 0,
        Dog = 1,
        Bird = 2,
        Rabbit = 3,
        Other = 4
    }
}
