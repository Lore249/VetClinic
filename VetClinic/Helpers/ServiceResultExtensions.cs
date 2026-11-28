using Microsoft.AspNetCore.Mvc;
using VetClinic.Services;

namespace VetClinic.Helpers
{
    // Servisten gelen başarısız sonucu arayüzün cevabına çeviren tek yer (eşleme tablosu):
    //
    //   ErrorType   MVC (HandleServiceError)     REST API (ToApiError)   MCP (ToMcpMessage)
    //   NotFound    404 (NotFound())             404                     nedeni açıklayan metin
    //   Forbidden   403 (Forbid())               403                     nedeni açıklayan metin
    //   Validation  formda ModelState hatası     400                     nedeni açıklayan metin
    //   Conflict    formda ModelState hatası     409                     nedeni açıklayan metin
    public static class ServiceResultExtensions
    {
        // MCP sütunu. Yapay zekâ asistanı bu metni okuyup kullanıcıya aktarır; servisin mesajı aynen korunur,
        // önüne yalnızca hatanın türü eklenir. Örnek: "Saat çakışması: Hekimin 10:00-10:30 arasında başka bir randevusu var."
        public static string ToMcpMessage<T>(this ServiceResult<T> result)
        {
            var kind = result.ErrorType switch
            {
                ErrorType.NotFound => "Bulunamadı",
                ErrorType.Forbidden => "Yetki yok",
                ErrorType.Conflict => "Saat çakışması",
                _ => "Kural ihlali"
            };

            return kind + ": " + result.ErrorMessage;
        }

        // REST API sütunu. Gövde, [ApiController]'ın otomatik 400 cevabıyla aynı standart biçimdedir
        // (ProblemDetails, RFC 7807): { "title": "Çakışma", "status": 409, "detail": "Hekimin 10:00-10:30 arasında ..." }.
        // Türkçe başlıklar Program.cs'teki ClientErrorMapping ayarından gelir.
        public static ObjectResult ToApiError<T>(this ControllerBase controller, ServiceResult<T> result)
        {
            var statusCode = result.ErrorType switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return controller.Problem(detail: result.ErrorMessage, statusCode: statusCode);
        }

        // "this Controller" parametresi bunu bir extension method yapar: controller içinden this.HandleServiceError(result) diye çağrılır.
        // 404 ya da 403 ise dönen sonuç doğrudan döndürülmelidir. null dönerse hata ModelState'e eklenmiştir ve
        // controller formu (view'ı) bu hatayla yeniden gösterir.
        public static IActionResult? HandleServiceError<T>(this Controller controller, ServiceResult<T> result)
        {
            switch (result.ErrorType)
            {
                case ErrorType.NotFound:
                    return controller.NotFound();

                case ErrorType.Forbidden:
                    return controller.Forbid();

                default:
                    controller.ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "İşlem yapılamadı.");
                    return null;
            }
        }
    }
}
