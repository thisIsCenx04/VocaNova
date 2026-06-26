// F057 — modal xác nhận xóa (soft-delete) cho danh sách từ vựng, thay window.confirm.
(function () {
    'use strict';

    var modal = document.getElementById('delete-modal');
    var confirmBtn = document.getElementById('delete-modal-confirm');
    var wordEl = document.getElementById('delete-modal-word');
    var pendingForm = null;

    function openModal(form) {
        pendingForm = form;
        if (wordEl) {
            var w = form.getAttribute('data-word');
            wordEl.textContent = w ? '“' + w + '”' : '';
            wordEl.hidden = !w;
        }
        if (modal) { modal.hidden = false; }
        if (confirmBtn) { confirmBtn.focus(); }
    }

    function closeModal() {
        if (modal) { modal.hidden = true; }
        pendingForm = null;
    }

    document.querySelectorAll('form.js-confirm').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (form.dataset.confirmed === 'true') { return; } // đã xác nhận → cho submit thật
            e.preventDefault();
            openModal(form);
        });
    });

    if (confirmBtn) {
        confirmBtn.addEventListener('click', function () {
            if (pendingForm) {
                pendingForm.dataset.confirmed = 'true';
                pendingForm.submit();
            }
        });
    }

    if (modal) {
        modal.querySelectorAll('[data-close]').forEach(function (el) {
            el.addEventListener('click', closeModal);
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal && !modal.hidden) { closeModal(); }
    });
})();
