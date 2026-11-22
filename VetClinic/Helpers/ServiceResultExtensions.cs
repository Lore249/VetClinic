using Microsoft.AspNetCore.Mvc;
using VetClinic.Services;

namespace VetClinic.Helpers
{
    // Servisten gelen başarısız sonucu MVC cevabına çeviren tek yer (eşleme tablosunun MVC sütunu):
    //   NotFound   -> 404 (NotFound())
    //   Forbidden  -> 403 (Forbid())
    //   Validation -> formda gösterilen ModelState hatası
    //   Conflict   -> formda gösterilen ModelState hatası
    public static class ServiceResultExtensions
    {
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
