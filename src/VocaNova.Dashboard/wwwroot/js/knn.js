// KNN Management: modal-based create/edit/delete flows without inserting forms into the table.
(function () {
    'use strict';

    var activeModal = null;
    var activeTrigger = null;

    function focusableElements(modal) {
        return Array.from(modal.querySelectorAll(
            'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href]'
        )).filter(function (element) { return !element.hidden; });
    }

    function openModal(modal, trigger) {
        if (!modal) { return; }
        activeModal = modal;
        activeTrigger = trigger || null;
        modal.hidden = false;
        document.body.classList.add('crud-modal-open');

        var firstField = modal.querySelector('.form-control');
        var firstFocusable = focusableElements(modal)[0];
        (firstField || firstFocusable || modal).focus();
    }

    function closeActiveModal() {
        if (!activeModal) { return; }

        var modal = activeModal;
        var trigger = activeTrigger;
        var form = modal.querySelector('form');

        modal.hidden = true;
        if (form) { form.reset(); }
        activeModal = null;
        activeTrigger = null;
        document.body.classList.remove('crud-modal-open');
        if (trigger) { trigger.focus(); }
    }

    document.querySelectorAll('[data-open-knn-modal]').forEach(function (button) {
        button.addEventListener('click', function () {
            openModal(document.getElementById(button.getAttribute('data-open-knn-modal')), button);
        });
    });

    document.querySelectorAll('[data-close-knn-modal]').forEach(function (button) {
        button.addEventListener('click', closeActiveModal);
    });

    document.querySelectorAll('[data-knn-modal]').forEach(function (modal) {
        modal.addEventListener('click', function (event) {
            if (event.target === modal) { closeActiveModal(); }
        });
    });

    var deleteModal = document.getElementById('knn-delete-modal');
    var deleteId = document.getElementById('knn-delete-id');
    var deleteText = document.getElementById('knn-delete-text');

    function escapeHtml(value) {
        var element = document.createElement('div');
        element.textContent = value == null ? '' : value;
        return element.innerHTML;
    }

    document.querySelectorAll('.js-knn-delete').forEach(function (button) {
        button.addEventListener('click', function () {
            if (!deleteModal || !deleteId || !deleteText) { return; }
            var id = button.getAttribute('data-id');
            var name = button.getAttribute('data-name') || ('item #' + id);
            var template = deleteModal.getAttribute('data-message') || 'Delete {0}?';
            deleteId.value = id;
            deleteText.innerHTML = template.replace('{0}', '<strong>' + escapeHtml(name) + '</strong>');
            openModal(deleteModal, button);
        });
    });

    var resetModal = document.getElementById('knn-reset-modal');
    var resetConfirm = document.getElementById('knn-reset-confirm');
    var pendingResetForm = null;

    document.querySelectorAll('form.js-knn-reset-confirm').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (form.dataset.confirmed === 'true') { return; }
            event.preventDefault();
            pendingResetForm = form;
            openModal(resetModal, form.querySelector('button[type="submit"]'));
        });
    });

    if (resetConfirm) {
        resetConfirm.addEventListener('click', function () {
            if (!pendingResetForm) { return; }
            pendingResetForm.dataset.confirmed = 'true';
            pendingResetForm.requestSubmit();
        });
    }

    document.addEventListener('keydown', function (event) {
        if (!activeModal) { return; }

        if (event.key === 'Escape') {
            event.preventDefault();
            closeActiveModal();
            return;
        }

        if (event.key !== 'Tab') { return; }
        var focusable = focusableElements(activeModal);
        if (focusable.length === 0) {
            event.preventDefault();
            return;
        }

        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });
}());
