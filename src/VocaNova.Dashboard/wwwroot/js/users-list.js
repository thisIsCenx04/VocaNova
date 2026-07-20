// User Management list: disable/restore from the Action column via one shared confirmation modal.
// i18n: the localized strings come from data-* on #user-modal (rendered by @T); JS only injects the name.
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

    // Escape the (user-controlled) name before injecting it as HTML.
    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : s; return d.innerHTML; }

    function openFor(mode, id, name) {
        var d = modal.dataset;
        var nameHtml = '<strong>' + esc(name || ('user #' + id)) + '</strong>';
        if (mode === 'restore') {
            form.action = '/users/' + id + '/restore';
            title.textContent = d.restoreTitle;
            text.innerHTML = (d.restoreText || '{0}').replace('{0}', nameHtml);
            confirm.textContent = d.restoreConfirm;
            confirm.className = 'btn-primary';
            icon.className = 'modal-icon ok';
        } else {
            form.action = '/users/' + id + '/deactivate';
            title.textContent = d.disableTitle;
            text.innerHTML = (d.disableText || '{0}').replace('{0}', nameHtml);
            confirm.textContent = d.disableConfirm;
            confirm.className = 'btn-warning';
            icon.className = 'modal-icon';
        }
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
