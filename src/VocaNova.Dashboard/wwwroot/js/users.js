// F060 — User detail tabs + delete confirm.
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
                panels.forEach(function (p) {
                    p.classList.toggle('active', p.getAttribute('data-tab') === target);
                });
            });
        });
    }

    document.querySelectorAll('form.js-confirm').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm') || 'Are you sure?')) {
                e.preventDefault();
            }
        });
    });
})();
