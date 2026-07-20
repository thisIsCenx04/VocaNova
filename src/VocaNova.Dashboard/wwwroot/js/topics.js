// F061 — Topics: delete confirmation.
(function () {
    'use strict';

    document.querySelectorAll('form.js-confirm').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!window.confirm(form.getAttribute('data-confirm') || 'Are you sure?')) {
                event.preventDefault();
            }
        });
    });
}());
