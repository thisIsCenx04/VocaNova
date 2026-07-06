(function () {
    "use strict";

    // Reveal the "new meaning" block.
    var addBtn = document.getElementById("add-meaning-btn");
    var newBlock = document.getElementById("new-meaning");
    if (addBtn && newBlock) {
        addBtn.addEventListener("click", function () {
            newBlock.hidden = false;
            addBtn.hidden = true;
            var firstField = newBlock.querySelector("textarea");
            if (firstField) {
                firstField.focus();
            }
        });
    }

    // Active/Inactive label follows the toggle.
    var toggle = document.getElementById("isActive");
    var label = document.getElementById("status-label");
    if (toggle && label) {
        toggle.addEventListener("change", function () {
            label.textContent = toggle.checked
                ? (label.dataset.active || "Active")
                : (label.dataset.inactive || "Inactive");
        });
    }

    // Thêm dòng ví dụ (clone từ template) — lưu ý: API chưa lưu ví dụ, đây là UI theo thiết kế.
    var tpl = document.getElementById("example-row-tpl");
    document.querySelectorAll(".add-example-link").forEach(function (link) {
        link.addEventListener("click", function () {
            if (!tpl) { return; }
            var block = link.closest(".meaning-block");
            var rows = block ? block.querySelector(".example-rows") : null;
            if (!rows) { return; }
            rows.appendChild(tpl.content.cloneNode(true));
            var added = rows.lastElementChild;
            if (added) {
                // Gắn ví dụ mới vào đúng sense (theo data-sense-idx của block).
                var idxField = added.querySelector('input[name="exampleSenseIdx"]');
                if (idxField) { idxField.value = block.getAttribute("data-sense-idx") || "0"; }
            }
            var firstField = added ? added.querySelector("textarea") : null;
            if (firstField) { firstField.focus(); }
        });
    });

    // Xóa dòng ví dụ (event delegation).
    // R02: nút xóa ví dụ ĐÃ LƯU bị khóa tạm thời (.is-locked/disabled) để tránh mất dữ liệu;
    // chỉ cho phép hủy dòng ví dụ MỚI thêm (chưa lưu).
    document.addEventListener("click", function (e) {
        var btn = e.target.closest ? e.target.closest(".example-remove") : null;
        if (btn && !btn.disabled && !btn.classList.contains("is-locked")) {
            var row = btn.closest(".example-row");
            if (row) { row.remove(); }
        }
    });

    // Play pronunciation audio.
    var audioEl = null;
    document.querySelectorAll(".audio-btn[data-audio]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var url = btn.getAttribute("data-audio");
            if (!url) {
                return;
            }
            if (!audioEl) {
                audioEl = new Audio();
            }
            audioEl.src = url;
            audioEl.play().catch(function () { /* ignore playback errors */ });
        });
    });
})();
