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
        form.setAttribute('aria-label', 'Go to page');
        form.setAttribute('novalidate', 'novalidate');
        form.innerHTML = '<span class="page-jump-controls"><label>Page <input type="number" min="1" max="' + total
            + '" placeholder="' + current + '" aria-label="Page number"></label>'
            + '<button type="submit">Go</button></span>'
            + '<span class="page-jump-error" role="alert" hidden>Please enter a page number from 1 to ' + total + '.</span>';

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
