// Admin rapor sayfası: GET /api/reports/services ucunu jQuery AJAX ile çağırır ve tabloyu doldurur.
// İstek aynı siteye gittiği için tarayıcı oturum çerezini kendiliğinden ekler; API, Admin rolünü bu çerezden görür
// (Program.cs'teki policy scheme: Authorization başlığı yoksa cookie kullanılır). Ayrıca token gerekmez.
$(function () {
    var $form = $("#report-form");
    if ($form.length === 0) {
        return; // Bu sayfa rapor sayfası değil.
    }

    var $body = $("#report-body");
    var $totals = $("#report-totals");
    var $message = $("#report-message");

    // 1250.5 -> "1.250,50 ₺" (tarayıcının Türkçe sayı biçimiyle)
    function formatMoney(value) {
        return value.toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " ₺";
    }

    // Tek hücreli bilgi satırı (yükleniyor, kayıt yok).
    function showInfoRow(text) {
        $body.empty().append($("<tr>").append($("<td>").attr("colspan", 4).addClass("text-muted").text(text)));
        $totals.empty();
    }

    function loadReport() {
        $message.addClass("d-none");
        showInfoRow("Rapor yükleniyor...");

        // Yalnızca doldurulan tarihler gönderilir: ?from=2026-11-01&to=2026-11-30
        var query = {};
        var from = $("#report-from").val();
        var to = $("#report-to").val();
        if (from) {
            query.from = from;
        }
        if (to) {
            query.to = to;
        }

        // $.ajax isteği arka planda gönderir; sayfa yenilenmez. Cevap gelince done ya da fail çalışır.
        $.ajax({
            url: "/api/reports/services",
            type: "GET",
            data: query,
            dataType: "json"
        })
            .done(function (rows) {
                renderRows(rows);
            })
            .fail(function (xhr) {
                showInfoRow("");
                var text = "Rapor alınamadı. Lütfen tekrar deneyin.";
                if (xhr.status === 401 || xhr.status === 403) {
                    text = "Bu raporu görmek için yönetici olarak giriş yapmalısınız.";
                } else if (xhr.responseJSON && xhr.responseJSON.detail) {
                    // API'nin hata gövdesi (ProblemDetails) içindeki Türkçe açıklama.
                    text = xhr.responseJSON.detail;
                }
                $message.text(text).removeClass("d-none");
            });
    }

    function renderRows(rows) {
        if (rows.length === 0) {
            showInfoRow("Bu tarih aralığında randevu yok.");
            return;
        }

        $body.empty();
        var totalCount = 0;
        var totalCompleted = 0;
        var totalRevenue = 0;

        // .text() kullanılır, .html() değil: hizmet adı HTML olarak yorumlanmaz (XSS'e karşı).
        $.each(rows, function (index, row) {
            $("<tr>")
                .append($("<td>").text(row.serviceName))
                .append($("<td>").addClass("text-end").text(row.appointmentCount))
                .append($("<td>").addClass("text-end").text(row.completedCount))
                .append($("<td>").addClass("text-end").text(formatMoney(row.revenue)))
                .appendTo($body);

            totalCount += row.appointmentCount;
            totalCompleted += row.completedCount;
            totalRevenue += row.revenue;
        });

        $totals.empty().append(
            $("<tr>")
                .append($("<td>").text("Toplam"))
                .append($("<td>").addClass("text-end").text(totalCount))
                .append($("<td>").addClass("text-end").text(totalCompleted))
                .append($("<td>").addClass("text-end").text(formatMoney(totalRevenue))));
    }

    // Form sunucuya gönderilmesin; yerine AJAX isteği yapılsın.
    $form.on("submit", function (event) {
        event.preventDefault();
        loadReport();
    });

    // Sayfa açılınca tüm zamanların raporu hemen yüklenir.
    loadReport();
});
