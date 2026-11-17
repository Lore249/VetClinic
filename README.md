# Kerem'in Kliniği – Veteriner Kliniği Randevu ve Hasta Takip Sistemi

Sakarya Üniversitesi, 2026-2027 Güz dönemi Web Programlama dersi proje ödevi.

| Bilgi | Değer |
|---|---|
| Öğrenci numarası | B241210016 |
| Ad soyad | Kerem EROL |
| Ders grubu | 1. Öğretim A Grubu |
| GitHub deposu | https://github.com/Lore249/VetClinic |

## Tanıtım

Kerem'in Kliniği, tek bir veteriner kliniği için geliştirilmiş bir randevu ve hasta takip uygulamasıdır.

- **Hayvan sahipleri** üye olur, evcil hayvanlarını ve aşı kayıtlarını ekler, hizmet ve hekim seçerek randevu alır.
- **Sistem** randevuyu kliniğin çalışma saatlerine, hekimin müsaitliğine ve hizmetin süresine göre kontrol eder; uygun değilse nedenini açıklar. Randevu "Bekliyor" durumunda oluşur.
- **Veteriner hekimler** kendi randevularını onaylar ya da reddeder, tamamlanan randevuya muayene notu girer.
- **Yönetici (Admin)** klinik bilgilerini, çalışma saatlerini, hizmetleri, hekimleri ve randevuları yönetir, hizmet bazlı raporları görür.

Uygulama ASP.NET Core MVC (.NET 10) ile yazılmıştır. Veriler EF Core (Code First) ile SQL Server'da tutulur, kimlik doğrulama ASP.NET Core Identity ile yapılır. Uygulama ayrıca bir REST API ve bir MCP sunucusu sunar.
