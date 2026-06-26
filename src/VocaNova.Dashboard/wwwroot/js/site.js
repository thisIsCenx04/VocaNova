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

// Popup kết quả (thành công/lỗi) — đóng bằng nút OK / click nền / Esc.
(function () {
    'use strict';

    var modal = document.getElementById('result-modal');
    if (!modal) { return; }

    function close() { modal.remove(); }

    var ok = document.getElementById('result-ok');
    if (ok) { ok.addEventListener('click', close); ok.focus(); }
    modal.addEventListener('click', function (e) { if (e.target === modal) { close(); } });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { close(); } });
})();
