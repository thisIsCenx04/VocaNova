// VocaNova Dashboard — base script (F055).
// Đóng sidebar drawer (mobile) sau khi chọn một mục điều hướng.
(function () {
    'use strict';

    var toggle = document.getElementById('sidebar-toggle');
    if (!toggle) {
        return;
    }

    document.querySelectorAll('.sidebar-nav a').forEach(function (link) {
        link.addEventListener('click', function () {
            toggle.checked = false;
        });
    });
})();

// Submit page-size controls consistently across all paginated lists.
(function () {
    'use strict';

    document.querySelectorAll('[data-page-size-select]').forEach(function (select) {
        select.addEventListener('change', function () {
            if (select.form) {
                select.form.submit();
            }
        });
    });
})();

// Direct page jump shared by all paginated lists. Existing query filters are preserved.
(function () {
    'use strict';

    document.querySelectorAll('.pager[data-total-pages]').forEach(function (pager) {
        var total = Number(pager.getAttribute('data-total-pages'));
        var current = Number(pager.getAttribute('data-page'));
        if (!Number.isInteger(total) || total < 2) { return; }

        var form = document.createElement('form');
        form.className = 'page-jump';
        var i18n = document.body.dataset;
        var pageLabel = i18n.pageLabel || 'Page';
        var goLabel = i18n.goLabel || 'Go';
        var rangeMessage = (i18n.pageRangeMessage || 'Please enter a page number from 1 to {0}.').replace('{0}', total);
        form.setAttribute('aria-label', i18n.goToPageLabel || 'Go to page');
        form.setAttribute('novalidate', 'novalidate');
        form.innerHTML = '<span class="page-jump-controls"><label>' + pageLabel + ' <input type="number" min="1" max="' + total
            + '" placeholder="' + current + '" aria-label="' + (i18n.pageNumberLabel || 'Page number') + '"></label>'
            + '<button type="submit">' + goLabel + '</button></span>'
            + '<span class="page-jump-error" role="alert" hidden>' + rangeMessage + '</span>';

        pager.appendChild(form);

        var input = form.querySelector('input');
        var error = form.querySelector('.page-jump-error');
        var errorTimer;
        var fadeTimer;
        function hideError() {
            clearTimeout(errorTimer);
            clearTimeout(fadeTimer);
            error.hidden = true;
            error.classList.remove('is-fading');
        }
        function showError() {
            clearTimeout(errorTimer);
            clearTimeout(fadeTimer);
            error.hidden = false;
            error.classList.remove('is-fading');
            errorTimer = setTimeout(function () {
                error.classList.add('is-fading');
                fadeTimer = setTimeout(hideError, 250);
            }, 3000);
        }
        function isValidPage() {
            var value = Number(input.value);
            return input.value !== '' && Number.isInteger(value) && value >= 1 && value <= total;
        }
        input.addEventListener('input', function () {
            if (input.value === '') {
                hideError();
                return;
            }

            var value = Number(input.value);
            if (!Number.isFinite(value) || !Number.isInteger(value) || value < 1) {
                input.value = 1;
                showError();
                return;
            }
            if (value > total) {
                input.value = total;
                showError();
                return;
            }

            hideError();
        });
        input.addEventListener('focus', function () {
            showError();
        });

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            if (!isValidPage()) {
                showError();
                input.focus();
                return;
            }
            hideError();
            var page = Number(input.value);
            var url = new URL(window.location.href);
            url.searchParams.set('page', String(page));
            window.location.assign(url.toString());
        });
    });
})();

// Shared toast and confirmation modal used across dashboard pages.
(function () {
    'use strict';

    var toast = document.getElementById('global-toast');
    var toastTimer;
    window.VocaNovaDashboard = window.VocaNovaDashboard || {};
    window.VocaNovaDashboard.notify = function (message, success) {
        if (!toast) { return; }
        window.clearTimeout(toastTimer);
        toast.textContent = message || '';
        toast.className = 'detail-toast ' + (success ? 'toast-ok' : 'toast-err');
        toast.hidden = false;
        toastTimer = window.setTimeout(function () { toast.hidden = true; }, 3500);
    };

    var modal = document.getElementById('global-confirm-modal');
    var title = document.getElementById('global-confirm-title');
    var message = document.getElementById('global-confirm-message');
    var confirmButton = document.getElementById('global-confirm-submit');
    var pendingForm;
    var pendingSubmitter;
    var previousFocus;
    if (!modal || !confirmButton) { return; }

    function closeModal() {
        modal.hidden = true;
        pendingForm = null;
        pendingSubmitter = null;
        if (previousFocus) { previousFocus.focus(); }
        previousFocus = null;
    }

    document.addEventListener('submit', function (event) {
        var form = event.target;
        var submitter = event.submitter;
        var source = submitter && submitter.hasAttribute('data-confirm-message') ? submitter : form;
        var confirmMessage = source.getAttribute('data-confirm-message');
        if (!confirmMessage || form.dataset.confirmed === 'true') { return; }
        event.preventDefault();
        pendingForm = form;
        pendingSubmitter = submitter;
        previousFocus = submitter || document.activeElement;
        title.textContent = source.getAttribute('data-confirm-title') || title.textContent;
        message.textContent = confirmMessage;
        confirmButton.textContent = source.getAttribute('data-confirm-button') || confirmButton.textContent;
        confirmButton.classList.toggle('btn-danger-solid', source.getAttribute('data-confirm-danger') === 'true');
        confirmButton.classList.toggle('btn-primary', source.getAttribute('data-confirm-danger') !== 'true');
        modal.hidden = false;
        confirmButton.focus();
    });

    confirmButton.addEventListener('click', function () {
        if (!pendingForm) { return; }
        var form = pendingForm;
        var submitter = pendingSubmitter;
        form.dataset.confirmed = 'true';
        modal.hidden = true;
        form.requestSubmit(submitter || undefined);
    });
    modal.querySelectorAll('[data-global-confirm-close]').forEach(function (button) { button.addEventListener('click', closeModal); });
    modal.addEventListener('click', function (event) { if (event.target === modal) { closeModal(); } });
    document.addEventListener('keydown', function (event) { if (event.key === 'Escape' && !modal.hidden) { closeModal(); } });
})();

// Popup kết quả (thành công/lỗi) — đóng bằng nút OK / click nền / Esc.
(function () {
    'use strict';

    var modal = document.getElementById('result-modal');
    if (!modal) { return; }

    function close() { modal.remove(); }

    var ok = document.getElementById('result-ok');
    if (ok) { ok.addEventListener('click', close); ok.focus(); }
    modal.addEventListener('click', function (e) { if (e.target === modal) { close(); } });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { close(); } });
})();
