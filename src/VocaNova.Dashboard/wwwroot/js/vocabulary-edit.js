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
            label.textContent = toggle.checked ? "Active" : "Inactive";
        });
    }

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
