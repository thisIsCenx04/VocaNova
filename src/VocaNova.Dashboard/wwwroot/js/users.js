// F060 — User detail: tab toggle + Disable confirmation modal.
(function () {
    'use strict';

    var tabs = document.getElementById('user-tabs');
    if (tabs) {
        var buttons = tabs.querySelectorAll('.tab-btn');
        var panels = tabs.querySelectorAll('.tab-panel');
        buttons.forEach(function (btn) {
            btn.addEventListener('click', function () {
                var target = btn.getAttribute('data-tab');
                buttons.forEach(function (b) { b.classList.toggle('active', b === btn); });
                panels.forEach(function (p) { p.classList.toggle('active', p.getAttribute('data-tab') === target); });
            });
        });
    }

    var modal = document.getElementById('disable-modal');
    if (modal) {
        var open = document.querySelector('[data-open-disable]');
        function show() { modal.hidden = false; }
        function hide() { modal.hidden = true; }
        if (open) { open.addEventListener('click', show); }
        modal.querySelectorAll('[data-close-disable]').forEach(function (b) { b.addEventListener('click', hide); });
        modal.addEventListener('click', function (e) { if (e.target === modal) { hide(); } });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { hide(); } });
    }
})();
