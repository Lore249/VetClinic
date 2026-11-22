namespace VetClinic.Services
{
    // Başarısız bir servis sonucunun türü. Her arayüz bu türe bakarak kendi cevabını üretir:
    // MVC (Helpers/ServiceResultExtensions), REST API (HTTP durum kodu) ve MCP (açıklayıcı metin).
    public enum ErrorType
    {
        // Başarılı sonuç; hata yok.
        None = 0,

        // Bir kural ihlal edildi (geçmiş tarih, çalışma saati dışı, geçersiz durum geçişi vb.).
        Validation = 1,

        // Kayıt yok ya da başka bir kullanıcıya ait; ikisi bilerek aynı cevabı alır.
        NotFound = 2,

        // Kullanıcı kaydı görebiliyor ama bu işlemi yapmaya yetkisi yok.
        Forbidden = 3,

        // Zaman çakışması (hekimin ya da hayvanın aynı saatte başka randevusu var).
        Conflict = 4
    }
}
