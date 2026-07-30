(function () {
    "use strict";

    var themeInput = document.getElementById("theme-input");
    var languageInput = document.getElementById("language-input");

    // Chọn theme: tô đậm thẻ được chọn, cập nhật hidden input (chưa lưu tới khi Save Changes).
    document.querySelectorAll(".js-theme").forEach(function (card) {
        card.addEventListener("click", function () {
            document.querySelectorAll(".js-theme").forEach(function (c) { c.classList.remove("is-selected"); });
            card.classList.add("is-selected");
            if (themeInput) { themeInput.value = card.getAttribute("data-theme"); }
        });
    });

    // Chọn ngôn ngữ.
    document.querySelectorAll(".js-language").forEach(function (row) {
        row.addEventListener("click", function () {
            document.querySelectorAll(".js-language").forEach(function (r) { r.classList.remove("is-selected"); });
            row.classList.add("is-selected");
            if (languageInput) { languageInput.value = row.getAttribute("data-language"); }
        });
    });

    // Xác nhận trước các hành động không hoàn tác được (reset cấu hình AI grading).
    document.querySelectorAll("form.js-confirm").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.getAttribute("data-confirm") || "Are you sure?")) {
                event.preventDefault();
            }
        });
    });
})();
