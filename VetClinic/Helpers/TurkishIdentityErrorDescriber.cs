using Microsoft.AspNetCore.Identity;

namespace VetClinic.Helpers
{
    // Identity hata mesajlarını varsayılan olarak İngilizce üretir.
    // Yalnızca bu uygulamada karşılaşılabilecek hatalar Türkçeleştirildi; diğerleri DefaultError'a düşmez, İngilizce kalır.
    // Kullanıcı adı olarak e-posta kaydedildiği için kullanıcı adı mesajları e-postadan söz eder.
    public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new IdentityError { Code = nameof(DefaultError), Description = "Beklenmeyen bir hata oluştu." };
        }

        public override IdentityError ConcurrencyFailure()
        {
            return new IdentityError { Code = nameof(ConcurrencyFailure), Description = "Kayıt bu sırada başka biri tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin." };
        }

        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError { Code = nameof(DuplicateUserName), Description = $"'{userName}' adresiyle kayıtlı bir hesap zaten var." };
        }

        public override IdentityError InvalidUserName(string? userName)
        {
            return new IdentityError { Code = nameof(InvalidUserName), Description = "E-posta adresi yalnızca İngilizce harf, rakam ve - . _ @ + karakterlerini içerebilir." };
        }

        public override IdentityError InvalidEmail(string? email)
        {
            return new IdentityError { Code = nameof(InvalidEmail), Description = $"'{email}' geçerli bir e-posta adresi değil." };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalıdır." };
        }
    }
}
