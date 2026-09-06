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

        var mediaType = "image";
        var mediaQuery = document.getElementById("media-suggest-query");
        var mediaSearch = document.getElementById("media-suggest-search");
        var mediaStatus = document.getElementById("media-suggest-status");
        var mediaResults = document.getElementById("media-suggest-results");

        function mediaField(item, snake, camel) {
            return item ? (item[snake] || item[camel] || "") : "";
        }

        function setMediaStatus(message, isError) {
            if (!mediaStatus) { return; }
            mediaStatus.textContent = message || "";
            mediaStatus.classList.toggle("is-error", !!isError);
        }

        function setImagePreview(url, title) {
            var imagePreview = document.getElementById("edit-image-preview");
            if (!imagePreview) { return; }
            imagePreview.innerHTML = '<div class="word-image-review">' +
                '<a class="word-image-preview" target="_blank" rel="noopener"><img class="word-image" alt=""></a>' +
                '<div class="word-image-review-meta"><span>' + editForm.dataset.labelImagePreview + '</span>' +
                '<button type="button" class="btn-icon btn-danger edit-media-delete" data-delete-media="image">' + editForm.dataset.labelDelete + '</button></div></div>';
            var link = imagePreview.querySelector("a");
            var image = imagePreview.querySelector("img");
            if (link) { link.href = url; }
            if (image) {
                image.src = url;
                image.alt = title || "";
            }
            if (imageUrlInput) { imageUrlInput.value = url; }
            var picker = editForm.querySelector('[data-file-picker="edit-image-file"]');
            if (picker) { picker.textContent = editForm.dataset.labelReplace; }
        }

        function applySuggestedImage(url, title, button) {
            if (!url) {
                showToast(editForm.dataset.msgRequestFailed, false);
                return;
            }
            var data = new URLSearchParams();
            data.append("imageUrl", url);
            if (tokenInput) { data.append("__RequestVerificationToken", tokenInput.value); }
            if (button) { button.disabled = true; }
            fetch("/vocabulary/" + wordId + "/image/suggested", {
                method: "POST",
                body: data,
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" }
            })
                .then(function (response) {
                    if (!response.ok) { throw new Error(); }
                    return response.json();
                })
                .then(function (result) {
                    showToast(result.message || editForm.dataset.msgImageApplied, result.success);
                    if (result.success) { setImagePreview(result.imageUrl || url, title); }
                })
                .catch(function () { showToast(editForm.dataset.msgRequestFailed, false); })
                .finally(function () { if (button) { button.disabled = false; } });
        }

        function copyText(text, button) {
            if (!text) { return; }
            var done = function () {
                if (!button) { return; }
                var oldText = button.textContent;
                button.textContent = editForm.dataset.labelCopied || "Copied";
                window.setTimeout(function () { button.textContent = oldText; }, 1200);
            };
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(done).catch(function () { showToast(editForm.dataset.msgRequestFailed, false); });
                return;
            }
            var temp = document.createElement("textarea");
            temp.value = text;
            temp.setAttribute("readonly", "readonly");
            temp.style.position = "absolute";
            temp.style.left = "-9999px";
            document.body.appendChild(temp);
            temp.select();
            try {
                document.execCommand("copy");
                done();
            } catch (error) {
                showToast(editForm.dataset.msgRequestFailed, false);
            }
            document.body.removeChild(temp);
        }

        function renderMediaResults(items) {
            if (!mediaResults) { return; }
            mediaResults.replaceChildren();
            if (!items || items.length === 0) {
                setMediaStatus(editForm.dataset.msgMediaEmpty, false);
                return;
            }
            setMediaStatus("", false);
            items.forEach(function (item) {
                var previewUrl = mediaField(item, "preview_url", "previewUrl");
                var fullUrl = mediaField(item, "full_size_url", "fullSizeUrl") || previewUrl;
                var sourceUrl = mediaField(item, "source_url", "sourceUrl");
                var creatorName = mediaField(item, "creator_name", "creatorName");
                var creatorUrl = mediaField(item, "creator_url", "creatorUrl");
                var title = mediaField(item, "title", "title") || "Pexels media";
                var itemType = mediaField(item, "media_type", "mediaType") || mediaType;

                var card = document.createElement("article");
                card.className = "media-suggest-card";

                var preview = document.createElement("a");
                preview.className = "media-suggest-preview";
                preview.href = sourceUrl || fullUrl;
                preview.target = "_blank";
                preview.rel = "noopener";
                var image = document.createElement("img");
                image.src = previewUrl;
                image.alt = title;
                image.loading = "lazy";
                preview.appendChild(image);
                if (itemType === "video") {
                    var badge = document.createElement("span");
                    badge.className = "media-video-badge";
                    badge.textContent = "Video";
                    preview.appendChild(badge);
                }
                card.appendChild(preview);

                var body = document.createElement("div");
                body.className = "media-suggest-card-body";
                var name = document.createElement("p");
                name.className = "media-suggest-card-title";
                name.textContent = title;
                body.appendChild(name);
                if (creatorName) {
                    var credit = document.createElement(creatorUrl ? "a" : "span");
                    credit.className = "media-suggest-card-credit";
                    credit.textContent = "by " + creatorName;
                    if (creatorUrl) {
                        credit.href = creatorUrl;
                        credit.target = "_blank";
                        credit.rel = "noopener";
                    }
                    body.appendChild(credit);
                }

                var actions = document.createElement("div");
                actions.className = "media-suggest-actions";
                if (itemType === "image") {
                    var useBtn = document.createElement("button");
                    useBtn.type = "button";
                    useBtn.className = "btn-primary btn-sm";
                    useBtn.textContent = editForm.dataset.labelUseImage || "Use image";
                    useBtn.addEventListener("click", function () { applySuggestedImage(fullUrl, title, useBtn); });
                    actions.appendChild(useBtn);
                } else {
                    var open = document.createElement("a");
                    open.className = "btn-secondary btn-sm";
                    open.href = sourceUrl || fullUrl;
                    open.target = "_blank";
                    open.rel = "noopener";
                    open.textContent = editForm.dataset.labelOpenVideo || "Open video";
                    actions.appendChild(open);
                }
                var copy = document.createElement("button");
                copy.type = "button";
                copy.className = "btn-secondary btn-sm";
                copy.textContent = editForm.dataset.labelCopyLink || "Copy link";
                copy.addEventListener("click", function () { copyText(sourceUrl || fullUrl, copy); });
                actions.appendChild(copy);
                body.appendChild(actions);
                card.appendChild(body);
                mediaResults.appendChild(card);
            });
        }

        function loadMediaSuggestions() {
            if (!mediaSearch || !mediaResults) { return; }
            var params = new URLSearchParams();
            params.set("type", mediaType);
            params.set("limit", "8");
            if (mediaQuery && mediaQuery.value.trim()) { params.set("query", mediaQuery.value.trim()); }
            mediaSearch.disabled = true;
            mediaResults.replaceChildren();
            setMediaStatus(editForm.dataset.msgMediaLoading, false);
            fetch("/vocabulary/" + wordId + "/media-suggestions?" + params.toString(), {
                method: "GET",
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            })
                .then(function (response) {
                    if (!response.ok) { throw new Error(); }
                    return response.json();
                })
                .then(function (result) {
                    if (!result.success) {
                        setMediaStatus(result.message || editForm.dataset.msgRequestFailed, true);
                        return;
                    }
                    renderMediaResults(result.items || []);
                })
                .catch(function () { setMediaStatus(editForm.dataset.msgRequestFailed, true); })
                .finally(function () { mediaSearch.disabled = false; });
        }

        editForm.querySelectorAll("[data-media-type]").forEach(function (button) {
            button.addEventListener("click", function () {
                mediaType = button.dataset.mediaType === "video" ? "video" : "image";
                editForm.querySelectorAll("[data-media-type]").forEach(function (other) {
                    other.classList.toggle("active", other === button);
                });
                if (mediaResults) { mediaResults.replaceChildren(); }
                setMediaStatus(editForm.dataset.msgMediaReady, false);
            });
        });

        if (mediaSearch) {
            mediaSearch.addEventListener("click", loadMediaSuggestions);
        }
        if (mediaQuery) {
            mediaQuery.addEventListener("keydown", function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    loadMediaSuggestions();
                }
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
