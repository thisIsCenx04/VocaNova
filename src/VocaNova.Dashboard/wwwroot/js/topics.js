// F061 — Topics: toggle inline edit row + delete confirm.
(function () {
    'use strict';

    document.querySelectorAll('.js-edit-topic').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var row = document.getElementById('edit-row-' + btn.getAttribute('data-topic-id'));
            if (row) { row.hidden = !row.hidden; }
        });
    });

    document.querySelectorAll('.js-edit-cancel').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var row = document.getElementById('edit-row-' + btn.getAttribute('data-topic-id'));
            if (row) { row.hidden = true; }
        });
    });

    document.querySelectorAll('form.js-confirm').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm') || 'Are you sure?')) {
                e.preventDefault();
            }
        });
    });
})();
