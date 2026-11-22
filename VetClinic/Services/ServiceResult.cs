namespace VetClinic.Services
{
    // Servis metotlarının ortak dönüş tipi. Kural ihlalinde exception fırlatılmaz: neden ErrorMessage'da, türü ErrorType'tadır.
    // T, başarılı olunca dönen veridir (yeni kaydın id'si, bir liste vb.). Veri döndürmeyen işlemlerde bool kullanılır ve true döner.
    public class ServiceResult<T>
    {
        public bool Success { get; private set; }

        public T? Data { get; private set; }

        public string? ErrorMessage { get; private set; }

        public ErrorType ErrorType { get; private set; }

        // Nesne yalnızca aşağıdaki metotlarla oluşturulabilir; böylece "başarılı ama hata mesajlı" gibi tutarsız bir sonuç üretilemez.
        private ServiceResult()
        {
        }

        public static ServiceResult<T> Ok(T data)
        {
            return new ServiceResult<T> { Success = true, Data = data, ErrorType = ErrorType.None };
        }

        public static ServiceResult<T> Fail(ErrorType errorType, string errorMessage)
        {
            return new ServiceResult<T> { Success = false, ErrorType = errorType, ErrorMessage = errorMessage };
        }

        // Kısa yollar: servis kodunda ServiceResult<int>.NotFound("Hayvan bulunamadı.") gibi okunur.
        public static ServiceResult<T> Validation(string errorMessage)
        {
            return Fail(ErrorType.Validation, errorMessage);
        }

        public static ServiceResult<T> NotFound(string errorMessage)
        {
            return Fail(ErrorType.NotFound, errorMessage);
        }

        public static ServiceResult<T> Forbidden(string errorMessage)
        {
            return Fail(ErrorType.Forbidden, errorMessage);
        }

        public static ServiceResult<T> Conflict(string errorMessage)
        {
            return Fail(ErrorType.Conflict, errorMessage);
        }
    }
}
