// F058 — Vocabulary detail: inline AJAX cho sense edit/add, audio/image upload + delete.
(function () {
    'use strict';

    var root = document.getElementById('detail-root');
    if (!root) {
        return;
    }

    var wordId = root.getAttribute('data-word-id');
    var tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenField ? tokenField.value : '';

    function toast(message, ok) {
        var el = document.getElementById('detail-toast');
        if (!el) { return; }
        el.textContent = message;
        el.className = 'detail-toast ' + (ok ? 'toast-ok' : 'toast-err');
        el.hidden = false;
        window.setTimeout(function () { el.hidden = true; }, 3500);
    }

    // Gửi FormData kèm antiforgery token (header). Trả về promise JSON {success,message}.
    function postForm(url, formData) {
        return fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: formData
        }).then(function (res) { return res.json(); });
    }

    // ----- Sense: edit inline (không reload) -----
    root.querySelectorAll('.sense-item').forEach(function (item) {
        var editForm = item.querySelector('.js-sense-edit');
        var toggleBtn = item.querySelector('.js-edit-toggle');
        var cancelBtn = item.querySelector('.js-edit-cancel');

        if (toggleBtn && editForm) {
            toggleBtn.addEventListener('click', function () { editForm.hidden = false; toggleBtn.hidden = true; });
        }
        if (cancelBtn && editForm && toggleBtn) {
            cancelBtn.addEventListener('click', function () { editForm.hidden = true; toggleBtn.hidden = false; });
        }
        if (editForm) {
            editForm.addEventListener('submit', function (e) {
                e.preventDefault();
                var senseId = editForm.getAttribute('data-sense-id');
                postForm('/vocabulary/' + wordId + '/senses/' + senseId, new FormData(editForm))
                    .then(function (data) {
                        if (data.success) {
                            // Cập nhật hiển thị tại chỗ, không reload.
                            item.querySelector('.js-sense-def').textContent = editForm.querySelector('[name="englishDefinition"]').value;
                            item.querySelector('.js-sense-vi').textContent = editForm.querySelector('[name="vietnameseMeaning"]').value;
                            item.querySelector('.sense-class').textContent = editForm.querySelector('[name="wordClass"]').value;
                            item.querySelector('.sense-order').textContent = '#' + editForm.querySelector('[name="senseOrder"]').value;
                            editForm.hidden = true;
                            if (toggleBtn) { toggleBtn.hidden = false; }
                        }
                        toast(data.message, data.success);
                    })
                    .catch(function () { toast('Request failed.', false); });
            });
        }
    });

    // ----- Sense: add (reload sau khi thêm để lấy sense_id mới) -----
    var addForm = document.getElementById('add-sense-form');
    if (addForm) {
        addForm.addEventListener('submit', function (e) {
            e.preventDefault();
            postForm('/vocabulary/' + wordId + '/senses', new FormData(addForm))
                .then(function (data) {
                    toast(data.message, data.success);
                    if (data.success) { window.setTimeout(function () { window.location.reload(); }, 600); }
                })
                .catch(function () { toast('Request failed.', false); });
        });
    }

    // ----- Image / Audio upload (reload sau khi thành công) -----
    function wireUpload(formId, url) {
        var form = document.getElementById(formId);
        if (!form) { return; }
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            postForm(url, new FormData(form))
                .then(function (data) {
                    toast(data.message, data.success);
                    if (data.success) { window.setTimeout(function () { window.location.reload(); }, 600); }
                })
                .catch(function () { toast('Request failed.', false); });
        });
    }
    wireUpload('image-upload-form', '/vocabulary/' + wordId + '/image');
    wireUpload('audio-upload-form', '/vocabulary/' + wordId + '/audio');

    // ----- Phát audio phát âm (nút loa UK/US trên thẻ từ) -----
    var playerEl = null;
    root.querySelectorAll('.vd-audio-btn[data-audio]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var url = btn.getAttribute('data-audio');
            if (!url) { return; }
            if (!playerEl) { playerEl = new Audio(); }
            playerEl.src = url;
            playerEl.play().catch(function () { /* ignore playback errors */ });
        });
    });

    // ----- Audio delete (confirm + AJAX) -----
    root.querySelectorAll('form.js-confirm[data-ajax]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            if (!window.confirm(form.getAttribute('data-confirm') || 'Are you sure?')) { return; }
            postForm(form.getAttribute('data-ajax'), new FormData())
                .then(function (data) {
                    toast(data.message, data.success);
                    if (data.success) { window.setTimeout(function () { window.location.reload(); }, 600); }
                })
                .catch(function () { toast('Request failed.', false); });
        });
    });
})();
