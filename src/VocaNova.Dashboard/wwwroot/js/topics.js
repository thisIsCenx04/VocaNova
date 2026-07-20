// Topic Management: shared soft-delete confirmation used by the Action column.
(function () {
    'use strict';

    var modal = document.getElementById('topic-delete-modal');
    if (!modal) { return; }

    var form = document.getElementById('topic-delete-form');
    var message = document.getElementById('topic-delete-text');
    var lastTrigger = null;

    function escapeHtml(value) {
        var element = document.createElement('div');
        element.textContent = value == null ? '' : value;
        return element.innerHTML;
    }

    function openModal(button) {
        var id = button.getAttribute('data-id');
        var name = button.getAttribute('data-name') || ('topic #' + id);
        var template = modal.getAttribute('data-message') || 'Delete {0}?';

        lastTrigger = button;
        form.action = '/topics/' + id + '/delete';
        message.innerHTML = template.replace('{0}', '<strong>' + escapeHtml(name) + '</strong>');
        modal.hidden = false;
        modal.querySelector('[data-close-modal]').focus();
    }

    function closeModal() {
        modal.hidden = true;
        if (lastTrigger) { lastTrigger.focus(); }
        lastTrigger = null;
    }

    document.querySelectorAll('.js-topic-delete').forEach(function (button) {
        button.addEventListener('click', function () { openModal(button); });
    });

    modal.querySelectorAll('[data-close-modal]').forEach(function (button) {
        button.addEventListener('click', closeModal);
    });
    modal.addEventListener('click', function (event) {
        if (event.target === modal) { closeModal(); }
    });
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !modal.hidden) { closeModal(); }
    });
}());
