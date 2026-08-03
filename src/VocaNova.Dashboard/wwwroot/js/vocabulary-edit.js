(function () {
    "use strict";

    var editForm = document.getElementById("vocabulary-edit-form");
    if (editForm) {
        var wordId = editForm.dataset.wordId;
        var tokenInput = editForm.querySelector('input[name="__RequestVerificationToken"]');
        var imageUrlInput = document.getElementById("edit-image-url");
        var saveButton = editForm.querySelector('button[type="submit"]');
        var toastEl = document.getElementById("edit-toast");
        var activeUploads = 0;

        function showToast(message, success) {
            if (!toastEl) { return; }
            toastEl.textContent = message || editForm.dataset.msgRequestFailed || "Request failed.";
            toastEl.classList.toggle("toast-ok", !!success);
            toastEl.classList.toggle("toast-err", !success);
            toastEl.hidden = false;
            window.setTimeout(function () { toastEl.hidden = true; }, 3500);
        }

        function uploadFile(input, endpoint, requiredMessage) {
            if (!input.files || input.files.length === 0) {
                showToast(requiredMessage, false);
                return;
            }
            var data = new FormData();
            data.append("file", input.files[0]);
            if (tokenInput) { data.append("__RequestVerificationToken", tokenInput.value); }
            var button = editForm.querySelector('[data-file-picker="' + input.id + '"]');
            if (button) { button.disabled = true; }
            activeUploads++;
            if (saveButton) { saveButton.disabled = true; }
            fetch("/vocabulary/" + wordId + endpoint, { method: "POST", body: data, credentials: "same-origin" })
                .then(function (response) {
                    if (!response.ok) { throw new Error(); }
                    return response.json();
                })
                .then(function (result) {
                    showToast(result.message, result.success);
                    if (!result.success) { return; }

                    var objectUrl = URL.createObjectURL(input.files[0]);
                    if (endpoint === "/image") {
                        var imagePreview = document.getElementById("edit-image-preview");
                        if (imagePreview) {
                            imagePreview.innerHTML = '<div class="word-image-review">' +
                                '<a class="word-image-preview" target="_blank" rel="noopener"><img class="word-image" alt=""></a>' +
                                '<div class="word-image-review-meta"><span>' + editForm.dataset.labelImagePreview + '</span>' +
                                '<button type="button" class="btn-icon btn-danger edit-media-delete" data-delete-media="image">' + editForm.dataset.labelDelete + '</button></div></div>';
                            var link = imagePreview.querySelector("a");
                            var image = imagePreview.querySelector("img");
                            link.href = objectUrl;
                            image.src = objectUrl;
                            if (imageUrlInput) { imageUrlInput.value = result.imageUrl || ""; }
                        }
                    } else {
                        var audioPreview = document.getElementById("edit-audio-preview");
                        if (audioPreview) {
                            audioPreview.innerHTML = '<ul class="audio-list"><li class="audio-item">' +
                                '<span class="badge badge-muted">US</span><audio controls preload="none"></audio>' +
                                '<button type="button" class="btn-icon btn-danger edit-media-delete" data-delete-media="audio">' + editForm.dataset.labelDelete + '</button></li></ul>';
                            audioPreview.querySelector("audio").src = objectUrl;
                            audioPreview.querySelector("[data-delete-media]").dataset.audioId = result.audioId;
                        }
                    }
                    if (button) { button.textContent = editForm.dataset.labelReplace; }
                })
                .catch(function () { showToast(editForm.dataset.msgRequestFailed, false); })
                .finally(function () {
                    input.value = "";
                    if (button) { button.disabled = false; }
                    activeUploads--;
                    if (saveButton && activeUploads === 0) { saveButton.disabled = false; }
                });
        }

        editForm.querySelectorAll("[data-file-picker]").forEach(function (button) {
            var input = document.getElementById(button.dataset.filePicker);
            if (!input) { return; }
            button.addEventListener("click", function () { input.click(); });
            input.addEventListener("change", function () {
                var isImage = input.id === "edit-image-file";
                uploadFile(input, isImage ? "/image" : "/audio",
                    isImage ? editForm.dataset.msgImageRequired : editForm.dataset.msgAudioRequired);
            });
        });

        var deleteModal = document.getElementById("edit-media-delete-modal");
        var deleteMessage = document.getElementById("edit-media-delete-message");
        var deleteConfirm = document.getElementById("edit-media-delete-confirm");
        var pendingDelete = null;

        function closeDeleteModal() {
            if (deleteModal) { deleteModal.hidden = true; }
            pendingDelete = null;
        }

        editForm.addEventListener("click", function (event) {
            var button = event.target.closest("[data-delete-media]");
            if (!button) { return; }
            pendingDelete = button;
            var isImage = button.dataset.deleteMedia === "image";
            if (deleteMessage) {
                deleteMessage.textContent = isImage ? editForm.dataset.msgDeleteImage : editForm.dataset.msgDeleteAudio;
            }
            if (deleteModal) { deleteModal.hidden = false; }
        });

        if (deleteConfirm) {
            deleteConfirm.addEventListener("click", function () {
                if (!pendingDelete) { return; }
                var button = pendingDelete;
                var type = button.dataset.deleteMedia;
                var audioId = button.dataset.audioId;
                var endpoint = type === "image" ? "/image/delete" : "/audio/" + audioId + "/delete";
                closeDeleteModal();

                var data = new FormData();
                if (tokenInput) { data.append("__RequestVerificationToken", tokenInput.value); }
                fetch("/vocabulary/" + wordId + endpoint, { method: "POST", body: data, credentials: "same-origin" })
                    .then(function (response) {
                        if (!response.ok) { throw new Error(); }
                        return response.json();
                    })
                    .then(function (result) {
                        showToast(result.message, result.success);
                        if (!result.success) { return; }
                        var preview = document.getElementById(type === "image" ? "edit-image-preview" : "edit-audio-preview");
                        if (preview) { preview.innerHTML = '<p class="text-muted">' + (type === "image" ? editForm.dataset.labelNoImage : editForm.dataset.labelNoAudio) + '</p>'; }
                        if (type === "image" && imageUrlInput) { imageUrlInput.value = ""; }
                        var picker = editForm.querySelector('[data-file-picker="' + (type === "image" ? "edit-image-file" : "edit-audio-file") + '"]');
                        if (picker) { picker.textContent = type === "image" ? editForm.dataset.labelUploadImage : editForm.dataset.labelUploadAudio; }
                    })
                    .catch(function () { showToast(editForm.dataset.msgRequestFailed, false); });
            });
        }

        if (deleteModal) {
            deleteModal.querySelectorAll("[data-close]").forEach(function (element) {
                element.addEventListener("click", closeDeleteModal);
            });
        }
    }

    // Reveal the "new meaning" block.
    var addBtn = document.getElementById("add-meaning-btn");
    var newBlock = document.getElementById("new-meaning");
    if (addBtn && newBlock) {
        addBtn.addEventListener("click", function () {
            newBlock.hidden = false;
            addBtn.hidden = true;
            var firstField = newBlock.querySelector("textarea");
            if (firstField) {
                firstField.focus();
            }
        });
    }

    // Active/Inactive label follows the toggle.
    var toggle = document.getElementById("isActive");
    var label = document.getElementById("status-label");
    if (toggle && label) {
        toggle.addEventListener("change", function () {
            label.textContent = toggle.checked
                ? (label.dataset.active || "Active")
                : (label.dataset.inactive || "Inactive");
            label.classList.toggle("is-active", toggle.checked);
            label.classList.toggle("is-inactive", !toggle.checked);
        });
    }

    // Thêm dòng ví dụ (clone từ template) — lưu ý: API chưa lưu ví dụ, đây là UI theo thiết kế.
    var tpl = document.getElementById("example-row-tpl");
    document.querySelectorAll(".add-example-link").forEach(function (link) {
        link.addEventListener("click", function () {
            if (!tpl) { return; }
            var block = link.closest(".meaning-block");
            var rows = block ? block.querySelector(".example-rows") : null;
            if (!rows) { return; }
            rows.appendChild(tpl.content.cloneNode(true));
            var added = rows.lastElementChild;
            if (added) {
                // Gắn ví dụ mới vào đúng sense (theo data-sense-idx của block).
                var idxField = added.querySelector('input[name="exampleSenseIdx"]');
                if (idxField) { idxField.value = block.getAttribute("data-sense-idx") || "0"; }
            }
            var firstField = added ? added.querySelector("textarea") : null;
            if (firstField) { firstField.focus(); }
        });
    });

    // Xóa dòng ví dụ (event delegation).
    // R02: nút xóa ví dụ ĐÃ LƯU bị khóa tạm thời (.is-locked/disabled) để tránh mất dữ liệu;
    // chỉ cho phép hủy dòng ví dụ MỚI thêm (chưa lưu).
    document.addEventListener("click", function (e) {
        var btn = e.target.closest ? e.target.closest(".example-remove") : null;
        if (btn && !btn.disabled && !btn.classList.contains("is-locked")) {
            var row = btn.closest(".example-row");
            if (row) { row.remove(); }
        }
    });

    // Play pronunciation audio.
    var audioEl = null;
    document.querySelectorAll(".audio-btn[data-audio]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var url = btn.getAttribute("data-audio");
            if (!url) {
                return;
            }
            if (!audioEl) {
                audioEl = new Audio();
            }
            audioEl.src = url;
            audioEl.play().catch(function () { /* ignore playback errors */ });
        });
    });
})();
