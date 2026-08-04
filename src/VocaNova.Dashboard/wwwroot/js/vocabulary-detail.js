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
    // Localized fallback messages (rendered by @T).
    var MSG_FAIL = root.dataset.msgRequestFailed || 'Request failed.';
    var MSG_CONFIRM = root.dataset.msgConfirm || 'Are you sure?';
    var MSG_IMAGE_REQUIRED = root.dataset.msgImageRequired || 'Please choose an image file.';
    var MSG_AUDIO_REQUIRED = root.dataset.msgAudioRequired || 'Please choose an audio file.';

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
                    .catch(function () { toast(MSG_FAIL, false); });
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
                .catch(function () { toast(MSG_FAIL, false); });
        });
    }

    // ----- Image / Audio upload (reload sau khi thành công) -----
    function wireUpload(formId, url, requiredMessage) {
        var form = document.getElementById(formId);
        if (!form) { return; }
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var fileInput = form.querySelector('input[type="file"]');
            if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
                toast(requiredMessage, false);
                if (fileInput) { fileInput.focus(); }
                return;
            }
            postForm(url, new FormData(form))
                .then(function (data) {
                    toast(data.message, data.success);
                    if (data.success) { window.setTimeout(function () { window.location.reload(); }, 600); }
                })
                .catch(function () { toast(MSG_FAIL, false); });
        });
    }
    wireUpload('image-upload-form', '/vocabulary/' + wordId + '/image', MSG_IMAGE_REQUIRED);
    wireUpload('audio-upload-form', '/vocabulary/' + wordId + '/audio', MSG_AUDIO_REQUIRED);

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

    // ----- Image / audio delete (custom confirm modal + AJAX) -----
    var deleteModal = document.getElementById('detail-delete-modal');
    var deleteMessage = document.getElementById('detail-delete-message');
    var deleteConfirm = document.getElementById('detail-delete-confirm');
    var pendingDeleteForm = null;

    function closeDeleteModal() {
        if (deleteModal) { deleteModal.hidden = true; }
        pendingDeleteForm = null;
    }

    function submitDelete(form) {
        postForm(form.getAttribute('data-ajax'), new FormData())
            .then(function (data) {
                toast(data.message, data.success);
                if (data.success) { window.setTimeout(function () { window.location.reload(); }, 600); }
            })
            .catch(function () { toast(MSG_FAIL, false); });
    }

    root.querySelectorAll('form.js-confirm[data-ajax]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            pendingDeleteForm = form;
            if (deleteMessage) {
                deleteMessage.textContent = form.getAttribute('data-confirm') || MSG_CONFIRM;
            }
            if (deleteModal) {
                deleteModal.hidden = false;
                if (deleteConfirm) { deleteConfirm.focus(); }
            } else {
                submitDelete(form);
            }
        });
    });

    if (deleteConfirm) {
        deleteConfirm.addEventListener('click', function () {
            if (!pendingDeleteForm) { return; }
            var form = pendingDeleteForm;
            closeDeleteModal();
            submitDelete(form);
        });
    }

    if (deleteModal) {
        deleteModal.querySelectorAll('[data-close]').forEach(function (el) {
            el.addEventListener('click', closeDeleteModal);
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && deleteModal && !deleteModal.hidden) {
            closeDeleteModal();
        }
    });
})();
