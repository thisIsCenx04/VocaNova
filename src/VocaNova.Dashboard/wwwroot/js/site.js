// Dashboard client scripts.

// Theme toggle (F055.2): đổi data-theme trên <html> + lưu cookie để server render đúng theme lần sau.
(function () {
    var COOKIE = 'VocaNova.Dashboard.Theme';
    var toggle = document.getElementById('theme-toggle');
    if (!toggle) {
        return;
    }

    toggle.addEventListener('click', function () {
        var root = document.documentElement;
        var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-theme', next);
        document.cookie = COOKIE + '=' + next + ';path=/;max-age=31536000;samesite=lax';
    });
})();

// Toast (F055.4): vnToast(message, kind) — kind ∈ ok|err|info.
window.vnToast = function (message, kind) {
    if (!message) {
        return;
    }
    var stack = document.getElementById('toast-stack');
    if (!stack) {
        return;
    }
    var item = document.createElement('div');
    item.className = 'toast-item is-' + (kind || 'info');
    item.setAttribute('role', 'status');
    item.textContent = message;
    stack.appendChild(item);
    setTimeout(function () { item.remove(); }, 4000);
};

document.addEventListener('DOMContentLoaded', function () {
    // Toast từ server (TempData) khi vừa load trang.
    var stack = document.getElementById('toast-stack');
    if (stack && stack.dataset.toastMessage) {
        window.vnToast(stack.dataset.toastMessage, stack.dataset.toastKind || 'info');
    }

    var hasBootstrap = !!window.bootstrap;

    // Confirm modal: nút [data-confirm-url] → mở modal → submit form POST tới url đó.
    var confirmEl = document.getElementById('confirm-modal');
    if (confirmEl && hasBootstrap) {
        var confirmModal = new bootstrap.Modal(confirmEl);
        var confirmForm = document.getElementById('confirm-modal-form');
        var confirmTitle = document.getElementById('confirm-modal-title');
        var confirmMsg = document.getElementById('confirm-modal-message');
        var confirmOk = document.getElementById('confirm-modal-ok');

        document.body.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-confirm-url]');
            if (!trigger) {
                return;
            }
            e.preventDefault();
            confirmForm.setAttribute('action', trigger.getAttribute('data-confirm-url'));
            confirmForm.setAttribute('method', trigger.getAttribute('data-confirm-method') || 'post');
            if (confirmTitle && trigger.getAttribute('data-confirm-title')) {
                confirmTitle.textContent = trigger.getAttribute('data-confirm-title');
            }
            if (confirmMsg) {
                confirmMsg.textContent = trigger.getAttribute('data-confirm-message') || '';
            }
            if (confirmOk) {
                var danger = trigger.getAttribute('data-confirm-variant') === 'danger';
                confirmOk.className = 'btn ' + (danger ? 'btn-danger' : 'btn-primary');
                if (trigger.getAttribute('data-confirm-ok')) {
                    confirmOk.textContent = trigger.getAttribute('data-confirm-ok');
                }
            }
            confirmModal.show();
        });
    }

    // Form modal: nút [data-form-url] → nạp form (GET) → submit AJAX (POST).
    var formEl = document.getElementById('form-modal');
    if (formEl && hasBootstrap) {
        var formModal = new bootstrap.Modal(formEl);
        var formBody = formEl.querySelector('.modal-body');
        var formTitle = formEl.querySelector('.modal-title');

        document.body.addEventListener('click', function (e) {
            var trigger = e.target.closest('[data-form-url]');
            if (!trigger) {
                return;
            }
            e.preventDefault();
            if (formTitle) {
                formTitle.textContent = trigger.getAttribute('data-form-title') || '';
            }
            formBody.innerHTML = '<div class="skeleton skeleton-row"></div><div class="skeleton skeleton-row"></div>';
            formModal.show();
            fetch(trigger.getAttribute('data-form-url'), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.text(); })
                .then(function (html) { formBody.innerHTML = html; })
                .catch(function () { formBody.innerHTML = '<p class="text-danger">Không tải được biểu mẫu.</p>'; });
        });

        formEl.addEventListener('submit', function (e) {
            var form = e.target.closest('form');
            if (!form) {
                return;
            }
            e.preventDefault();
            fetch(form.getAttribute('action'), {
                method: form.getAttribute('method') || 'post',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (r) {
                var contentType = r.headers.get('content-type') || '';
                if (r.ok && contentType.indexOf('application/json') !== -1) {
                    return r.json().then(function (json) {
                        formModal.hide();
                        if (json && json.message) {
                            window.vnToast(json.message, 'ok');
                        }
                        if (form.getAttribute('data-reload') !== 'false') {
                            window.location.reload();
                        }
                    });
                }
                return r.text().then(function (html) { formBody.innerHTML = html; });
            }).catch(function () {
                window.vnToast('Có lỗi xảy ra. Thử lại.', 'err');
            });
        });
    }
});
