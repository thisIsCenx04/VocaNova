// VocaNova Dashboard — base script (F055).
// Đóng sidebar drawer (mobile) sau khi chọn một mục điều hướng.
(function () {
    'use strict';

    var toggle = document.getElementById('sidebar-toggle');
    if (!toggle) {
        return;
    }

    document.querySelectorAll('.sidebar-nav a').forEach(function (link) {
        link.addEventListener('click', function () {
            toggle.checked = false;
        });
    });
})();
