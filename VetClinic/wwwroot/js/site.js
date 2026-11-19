// jQuery Validation sayıları İngilizce biçimde (1250.50) bekler; uygulama ise tr-TR kültüründe çalışır ve
// sunucu ondalık ayırıcı olarak virgül bekler (1250,50). Bu yüzden number ve range kuralları virgülü kabul edecek
// şekilde değiştirildi.
// Doğrulama betikleri sayfanın sonunda (Scripts bölümünde) yüklendiği için değişiklik sayfa hazır olunca yapılır.
$(function () {
    if (!$.validator) {
        return; // Bu sayfada form doğrulaması yok.
    }

    // Yalnızca rakamlar ve isteğe bağlı olarak virgülden sonra ondalık kısım: 30, 1250, 1250,50
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?\d+(,\d+)?$/.test(value);
    };

    // Karşılaştırmadan önce virgül noktaya çevrilir; JavaScript'in parseFloat'ı yalnızca noktayı tanır.
    $.validator.methods.range = function (value, element, param) {
        var number = parseFloat(value.replace(",", "."));
        return this.optional(element) || (number >= param[0] && number <= param[1]);
    };
});
