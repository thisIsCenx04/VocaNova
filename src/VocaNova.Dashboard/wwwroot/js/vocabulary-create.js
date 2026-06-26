(function () {
    "use strict";

    var addBtn = document.getElementById("add-meaning-btn");
    var list = document.getElementById("meaning-list");
    if (!addBtn || !list) {
        return;
    }

    addBtn.addEventListener("click", function () {
        var first = list.querySelector(".meaning-card");
        if (!first) {
            return;
        }

        var clone = first.cloneNode(true);
        // Xóa giá trị đã nhập trong block nhân bản.
        clone.querySelectorAll("input, textarea").forEach(function (el) { el.value = ""; });
        clone.querySelectorAll("select").forEach(function (el) { el.selectedIndex = 0; });

        // Nút xóa block (block đầu tiên không có).
        if (!clone.querySelector(".meaning-remove")) {
            var remove = document.createElement("button");
            remove.type = "button";
            remove.className = "meaning-remove";
            remove.setAttribute("aria-label", "Remove meaning");
            remove.textContent = "✕";
            remove.addEventListener("click", function () { clone.remove(); });
            clone.appendChild(remove);
        }

        list.appendChild(clone);
        var firstField = clone.querySelector("input, textarea, select");
        if (firstField) {
            firstField.focus();
        }
    });
})();
