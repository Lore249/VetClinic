namespace VetClinic.Models.Dtos
{
    // Bir kaydın yalnızca id'si ve adı (ör. hekimin uzmanlıkları ve hizmetleri). İstemci id'yi başka uçlarda kullanabilir.
    public class IdNameDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
