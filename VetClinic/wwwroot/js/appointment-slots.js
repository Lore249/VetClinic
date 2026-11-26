// Randevu alma ekranı: boş saatler REST API'den (GET /api/veterinarians/available) jQuery $.get ile alınır ve sayfa
// yenilenmeden listelenir. Saatler sunucudaki IAppointmentService.GetAvailableSlotsAsync ile hesaplanır; burada yalnızca
// gösterilir. Her saat bir gönder düğmesidir: formaction ile hekimi ve saati taşıyan adrese POST eder, randevuyu yine
// sunucudaki CreateAsync tüm kurallarıyla oluşturur.
$(function () {
    var $searchForm = $("#slot-search-form");
    if ($searchForm.length === 0) {
        return; // Hayvanı olmayan üyede form gösterilmez.
    }

    var $bookingForm = $("#booking-form");
    var $slotArea = $("#slot-area");
    var $slotList = $("#slot-list");
    var $summary = $("#slot-summary");

    // "2026-11-30" -> "30.11.2026"
    function formatDate(isoDate) {
        return isoDate.split("-").reverse().join(".");
    }

    // Liste yerine tek bir bilgi kutusu (yükleniyor, uygun saat yok, hata).
    function showMessage(text, cssClass) {
        $slotList.empty().append($("<div>").addClass("alert mb-0 " + cssClass).text(text));
    }

    function loadSlots() {
        var serviceId = $("#ServiceId").val();
        var date = $("#Date").val(); // date kutusu değeri her dilde "2026-11-30" biçimindedir.

        // Arama anındaki seçimler POST formunun gizli alanlarına yazılır; saat düğmesine basılınca bunlar gönderilir.
        $bookingForm.find("input[name='PetId']").val($("#PetId").val());
        $bookingForm.find("input[name='ServiceId']").val(serviceId);
        $bookingForm.find("input[name='Date']").val(date);

        $summary.text($("#PetId option:selected").text() + " · " + $("#ServiceId option:selected").text() + " · " + formatDate(date));
        $slotArea.removeClass("d-none");
        showMessage("Boş saatler yükleniyor...", "alert-light");

        // $.get, $.ajax'ın GET için kısa yazımıdır. İkinci parametre sorgu dizesine çevrilir: ?serviceId=1&date=2026-11-30
        $.get("/api/veterinarians/available", { serviceId: serviceId, date: date })
            .done(function (slots) {
                renderSlots(slots, date);
            })
            .fail(function (xhr) {
                // API hata gövdesindeki (ProblemDetails) Türkçe açıklama varsa o gösterilir; ör. "Hizmet bulunamadı."
                var problem = xhr.responseJSON;
                var text = problem && problem.detail ? problem.detail : "Boş saatler alınamadı. Lütfen tekrar deneyin.";
                showMessage(text, "alert-danger");
            });
    }

    // API cevabı: [{ veterinarianId: 1, veterinarianName: "Vet. Hek. Selin Aydemir", startTimes: ["09:00:00", ...] }, ...]
    function renderSlots(slots, date) {
        if (slots.length === 0) {
            showMessage("Bu tarihte uygun saat yok: klinik kapalı olabilir, hizmeti veren hekimler o gün çalışmıyor olabilir, "
                + "saatler dolu olabilir ya da tarih geçmiş olabilir. Başka bir tarih deneyin.", "alert-info");
            return;
        }

        $slotList.empty();
        var actionUrl = $bookingForm.attr("action"); // "/Appointments/Create"

        $.each(slots, function (index, slot) {
            var $buttons = $("<div>").addClass("d-flex flex-wrap gap-2");

            $.each(slot.startTimes, function (timeIndex, time) {
                var hourMinute = time.substring(0, 5); // "09:00:00" -> "09:00"
                var url = actionUrl + "?veterinarianId=" + slot.veterinarianId
                    + "&startTime=" + encodeURIComponent(date + "T" + hourMinute);

                $("<button>")
                    .attr({ type: "submit", formaction: url })
                    .addClass("btn btn-outline-primary btn-sm")
                    .text(hourMinute)
                    .appendTo($buttons);
            });

            // .text() kullanılır, .html() değil: API'den gelen ad HTML olarak yorumlanmaz (XSS'e karşı).
            $("<div>").addClass("card kk-card mb-3")
                .append($("<div>").addClass("card-body")
                    .append($("<h3>").addClass("h6 mb-3").text(slot.veterinarianName))
                    .append($buttons))
                .appendTo($slotList);
        });
    }

    function allSelected() {
        return $("#PetId").val() && $("#ServiceId").val() && $("#Date").val();
    }

    // Seçim formu sunucuya gönderilmesin; alanlar geçerliyse (jQuery Validation) boş saatler AJAX ile alınsın.
    $searchForm.on("submit", function (event) {
        event.preventDefault();
        if ($searchForm.valid()) {
            loadSlots();
        }
    });

    // Seçimlerden biri değişince, üçü de doluysa liste kendiliğinden yenilenir; gizli alanlarda eski seçim kalmaz.
    $("#PetId, #ServiceId, #Date").on("change", function () {
        if (allSelected()) {
            loadSlots();
        }
    });

    // Sayfa seçimler dolu açıldıysa (adreste seçim varsa ya da randevu hatayla geri döndüyse) liste hemen yüklenir;
    // böylece bu arada dolan saat listede görünmez.
    if (allSelected()) {
        loadSlots();
    }
});
