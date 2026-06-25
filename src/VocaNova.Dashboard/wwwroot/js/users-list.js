// User Management list: disable/restore từ cột Action qua modal xác nhận (1 modal dùng chung).
(function () {
    'use strict';

    var modal = document.getElementById('user-modal');
    if (!modal) { return; }

    var form = document.getElementById('um-form');
    var title = document.getElementById('um-title');
    var text = document.getElementById('um-text');
    var icon = document.getElementById('um-icon');
    var confirm = document.getElementById('um-confirm');

    function show() { modal.hidden = false; }
    function hide() { modal.hidden = true; }

    function openFor(mode, id, name) {
        if (mode === 'restore') {
            form.action = '/users/' + id + '/restore';
            title.textContent = 'Restore user account';
            text.innerHTML = 'Restore <strong></strong> back to active? The user will regain access.';
            confirm.textContent = 'Restore';
            confirm.className = 'btn-primary';
            icon.className = 'modal-icon ok';
        } else {
            form.action = '/users/' + id + '/deactivate';
            title.textContent = 'Disable user account';
            text.innerHTML = 'Are you sure you want to disable <strong></strong>? This limits the user’s access to the system. You can restore it later.';
            confirm.textContent = 'Disable';
            confirm.className = 'btn-warning';
            icon.className = 'modal-icon';
        }
        text.querySelector('strong').textContent = name || ('user #' + id);
        show();
    }

    document.querySelectorAll('.js-disable').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openFor('disable', btn.getAttribute('data-id'), btn.getAttribute('data-name'));
        });
    });
    document.querySelectorAll('.js-restore').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openFor('restore', btn.getAttribute('data-id'), btn.getAttribute('data-name'));
        });
    });

    modal.querySelectorAll('[data-close-modal]').forEach(function (b) { b.addEventListener('click', hide); });
    modal.addEventListener('click', function (e) { if (e.target === modal) { hide(); } });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { hide(); } });
})();
