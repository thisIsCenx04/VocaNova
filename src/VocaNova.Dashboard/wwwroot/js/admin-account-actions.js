(function () {
    'use strict';

    var modal = document.getElementById('admin-action-modal');
    if (!modal) { return; }

    var title = document.getElementById('admin-action-title');
    var message = document.getElementById('admin-action-message');
    var confirmButton = document.getElementById('admin-action-confirm');
    var pendingForm = null;
    var lastTrigger = null;

    function closeModal() {
        modal.hidden = true;
        pendingForm = null;
        if (lastTrigger) { lastTrigger.focus(); }
        lastTrigger = null;
    }

    document.querySelectorAll('form[data-admin-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (form.dataset.confirmed === 'true') {
                delete form.dataset.confirmed;
                return;
            }

            event.preventDefault();
            pendingForm = form;
            lastTrigger = event.submitter || form.querySelector('[type="submit"]');
            title.textContent = form.dataset.confirmTitle || '';
            message.textContent = form.dataset.confirmMessage || '';
            confirmButton.textContent = form.dataset.confirmButton || '';
            confirmButton.classList.toggle('btn-danger-solid', form.dataset.confirmDanger === 'true');
            confirmButton.classList.toggle('btn-primary', form.dataset.confirmDanger !== 'true');
            modal.hidden = false;
            modal.querySelector('[data-close-modal]').focus();
        });
    });

    confirmButton.addEventListener('click', function () {
        if (!pendingForm) { return; }
        var form = pendingForm;
        form.dataset.confirmed = 'true';
        modal.hidden = true;
        form.requestSubmit(lastTrigger || undefined);
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

