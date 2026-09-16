(function () {
    "use strict";

    var editForm = document.getElementById("vocabulary-edit-form");
    function showToast(message, success) {
        if (!toastEl) { return; }
        toastEl.textContent = message || editForm.dataset.msgRequestFailed || "Request failed.";
        toastEl.classList.toggle("toast-ok", !!success);
        toastEl.classList.toggle("toast-err", !success);
        toastEl.hidden = false;
        window.setTimeout(function () { toastEl.hidden = true; }, 3500);
    }

    function stopAudio() {
        editForm.querySelectorAll("audio").forEach(function (audio) { audio.pause(); });
        if (audioEl) { audioEl.pause(); }
    }

    if (editForm) {
        var wordId = editForm.dataset.wordId;
        var tokenInput = editForm.querySelector('input[name="__RequestVerificationToken"]');
        var imageUrlInput = document.getElementById("edit-image-url");
        var saveButton = editForm.querySelector('button[type="submit"]');
        var toastEl = document.getElementById("edit-toast");
        var activeUploads = 0;
        editForm.addEventListener("word-video-busy", function (event) {
            activeUploads += event.detail ? 1 : -1;
            if (saveButton) { saveButton.disabled = activeUploads > 0; }
        });

        var allowSubmit = false;
        function hasPendingMedia() {
            var videoPending = document.querySelector("[data-pending-video]:not([hidden])");
            return (pendingImage && !pendingImage.hidden) || !!videoPending;
        }

        function waitForUploads() {
            return new Promise(function (resolve) {
                function check() {
                    if (activeUploads <= 0) { resolve(); return; }
                    window.setTimeout(check, 100);
                }
                check();
            });
        }

        editForm.addEventListener("submit", async function (event) {
            if (allowSubmit || !hasPendingMedia()) { return; }
            event.preventDefault();

            var started = false;
            var imageSave = pendingImage && !pendingImage.hidden ? pendingImage.querySelector("[data-image-save]") : null;
            var videoPending = document.querySelector("[data-pending-video]:not([hidden])");
            var videoSave = videoPending ? videoPending.querySelector("[data-video-save]") : null;

            if (imageSave && !imageSave.disabled) { imageSave.click(); started = true; }
            if (videoSave && !videoSave.disabled) { videoSave.click(); started = true; }
            if (!started) { showToast(editForm.dataset.msgRequestFailed, false); return; }

            await waitForUploads();
            if (hasPendingMedia()) { return; }

            allowSubmit = true;
            editForm.requestSubmit ? editForm.requestSubmit(saveButton) : editForm.submit();
        });

        function setAudioBusy(accent, busy) {
            if (!accent) { return; }
            var section = editForm.querySelector('[data-audio-section="' + accent + '"]');
            if (section) {
                section.querySelectorAll("button, input").forEach(function (control) { control.disabled = busy; });
            }
        }

        function uploadFile(input, endpoint, requiredMessage) {
            if (!input.files || input.files.length === 0) {
                showToast(requiredMessage, false);
                return;
            }
            var data = new FormData();
            var file = input.files[0];
            data.append("file", file);
            if (input.dataset.accent) { data.append("accent", input.dataset.accent); }
            if (tokenInput) { data.append("__RequestVerificationToken", tokenInput.value); }
            var button = editForm.querySelector('[data-file-picker="' + input.id + '"]');
            if (button) { button.disabled = true; }
            setAudioBusy(input.dataset.accent, true);
            activeUploads++;
            if (saveButton) { saveButton.disabled = true; }
            return fetch("/vocabulary/" + wordId + endpoint, { method: "POST", body: data, credentials: "same-origin" })
                .then(function (response) {
                    if (!response.ok) { throw new Error(); }
                    return response.json();
                })
                .then(function (result) {
                    showToast(result.message, result.success);
                    if (!result.success) { return; }

                    var objectUrl = endpoint === "/image" ? (result.imageUrl || URL.createObjectURL(file)) : result.audioUrl;
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
                            if (imageUrlInput && result.imageUrl) { imageUrlInput.value = result.imageUrl; }
                        }
                    } else {
                        var audioPreview = document.getElementById("edit-audio-preview-" + input.dataset.accent);
                        if (audioPreview && result.audioId && objectUrl) {
                            stopAudio();
                            audioPreview.innerHTML = '<ul class="audio-list"><li class="audio-item">' +
                                '<audio controls preload="none"></audio>' +
                                '<button type="button" class="btn-icon btn-danger edit-media-delete" data-delete-media="audio">' + editForm.dataset.labelDelete + '</button></li></ul>';
                            audioPreview.querySelector("audio").src = objectUrl;
                            audioPreview.querySelector("audio").setAttribute("aria-label", input.dataset.accent.toUpperCase());
                            audioPreview.querySelector("[data-delete-media]").dataset.audioId = result.audioId;
                            audioPreview.querySelector("[data-delete-media]").dataset.accent = input.dataset.accent;
                            if (input.dataset.accent === "uk") {
                                var pronunciation = editForm.querySelector(".audio-btn[data-audio]");
                                if (pronunciation) { pronunciation.dataset.audio = objectUrl; pronunciation.disabled = false; }
                            }
                        }
                    }
                    if (button) { button.textContent = editForm.dataset.labelReplace + (input.dataset.accent ? " (" + input.dataset.accent.toUpperCase() + ")" : ""); }
                    return true;
                })
                .catch(function () { showToast(editForm.dataset.msgRequestFailed, false); })
                .finally(function () {
                    input.value = "";
                    if (button) { button.disabled = false; }
                    setAudioBusy(input.dataset.accent, false);
                    activeUploads--;
                    if (saveButton && activeUploads === 0) { saveButton.disabled = false; }
                });
        }

        var pendingImage = editForm.querySelector("[data-pending-image]");
        var selectedImage = null;
        var imageObjectUrl = null;
        var imageBusy = false;

        function clearImageSelection() {
            if (!pendingImage) { return; }
            pendingImage.hidden = true;
            pendingImage.querySelector("img").removeAttribute("src");
            if (imageObjectUrl) { URL.revokeObjectURL(imageObjectUrl); imageObjectUrl = null; }
            selectedImage = null;
            document.getElementById("edit-image-file").value = "";
        }

        function selectImage(selection) {
            if (!pendingImage || imageBusy || (!selection.url && !selection.file)) { return; }
            clearImageSelection();
            selectedImage = selection;
            if (selection.file) { imageObjectUrl = URL.createObjectURL(selection.file); }
            pendingImage.querySelector("img").src = imageObjectUrl || selection.url;
            pendingImage.querySelector("[data-image-message]").textContent = "";
            pendingImage.hidden = false;
            pendingImage.scrollIntoView({ behavior: "smooth", block: "center" });
        }

        editForm.addEventListener("vocanova-media-selected", function (event) {
            if (event.detail.type === "image") { selectImage(event.detail); }
        });
        if (pendingImage) {
            pendingImage.querySelector("[data-image-cancel]").addEventListener("click", clearImageSelection);
            pendingImage.querySelector("[data-image-save]").addEventListener("click", async function () {
                if (!selectedImage || imageBusy) { return; }
                imageBusy = true;
                activeUploads++;
                if (saveButton) { saveButton.disabled = true; }
                pendingImage.querySelectorAll("button").forEach(function (button) { button.disabled = true; });
                var message = pendingImage.querySelector("[data-image-message]");
                message.textContent = editForm.dataset.msgMediaLoading;
                try {
                    var file = selectedImage.file || await window.downloadSuggestedMedia(selectedImage.url, "image");
                    var uploaded = await uploadFile({ files: [file], dataset: {}, id: "edit-image-file", value: "" }, "/image", editForm.dataset.msgImageRequired);
                    if (uploaded) { clearImageSelection(); }
                    else { message.textContent = editForm.dataset.msgRequestFailed; }
                } catch (_) { message.textContent = editForm.dataset.msgRequestFailed; }
                finally {
                    imageBusy = false;
                    activeUploads--;
                    if (saveButton) { saveButton.disabled = activeUploads > 0; }
                    pendingImage.querySelectorAll("button").forEach(function (button) { button.disabled = false; });
                }
            });
            window.addEventListener("pagehide", clearImageSelection);
        }

        editForm.querySelectorAll("[data-file-picker]").forEach(function (button) {
            var input = document.getElementById(button.dataset.filePicker);
            if (!input) { return; }
            button.addEventListener("click", function () { input.click(); });
            input.addEventListener("change", function () {
                var isImage = input.id === "edit-image-file";
                if (isImage) { selectImage({ file: input.files[0] }); return; }
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
            if (!button || button.disabled) { return; }
            pendingDelete = button;
            var isImage = button.dataset.deleteMedia === "image";
            if (deleteMessage) {
                deleteMessage.textContent = isImage ? editForm.dataset.msgDeleteImage : editForm.dataset.msgDeleteAudio;
            }
            if (deleteModal) { deleteModal.hidden = false; if (deleteConfirm) { deleteConfirm.focus(); } }
        });

        if (deleteConfirm) {
            deleteConfirm.addEventListener("click", function () {
                if (!pendingDelete) { return; }
                var button = pendingDelete;
                var type = button.dataset.deleteMedia;
                var audioId = button.dataset.audioId;
                var accent = button.dataset.accent;
                var endpoint = type === "image" ? "/image/delete" : "/audio/" + audioId + "/delete";
                closeDeleteModal();
                setAudioBusy(accent, true);
                activeUploads++;
                if (saveButton) { saveButton.disabled = true; }

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
                        if (type === "audio") {
                            stopAudio();
                            if (accent === "uk") {
                                var pronunciation = editForm.querySelector(".audio-btn[data-audio]");
                                if (pronunciation) { pronunciation.dataset.audio = ""; pronunciation.disabled = true; }
                            }
                        }
                        var preview = document.getElementById(type === "image" ? "edit-image-preview" : "edit-audio-preview-" + accent);
                        if (preview) { preview.innerHTML = '<p class="text-muted">' + (type === "image" ? editForm.dataset.labelNoImage : editForm.dataset.labelNoAudio) + '</p>'; }
                        if (type === "image" && imageUrlInput) { imageUrlInput.value = ""; }
                        var picker = editForm.querySelector('[data-file-picker="' + (type === "image" ? "edit-image-file" : "edit-audio-file-" + accent) + '"]');
                        if (picker) { picker.textContent = type === "image" ? editForm.dataset.labelUploadImage : editForm.dataset.labelUploadAudio + " (" + accent.toUpperCase() + ")"; }
                    })
                    .catch(function () { showToast(editForm.dataset.msgRequestFailed, false); })
                    .finally(function () {
                        setAudioBusy(accent, false);
                        activeUploads--;
                        if (saveButton && activeUploads === 0) { saveButton.disabled = false; }
                    });
            });
        }

        if (deleteModal) {
            deleteModal.querySelectorAll("[data-close]").forEach(function (element) {
                element.addEventListener("click", closeDeleteModal);
            });
        }

        editForm.querySelectorAll("[data-media-suggestions]").forEach(function (section) {
            var mediaType = section.dataset.mediaSuggestions;
            var mediaRetry = section.querySelector("[data-media-retry]");
            var mediaStatus = section.querySelector("[data-media-status]");
            var mediaResults = section.querySelector("[data-media-results]");

            function mediaField(item, snake, camel) {
                return item ? (item[snake] || item[camel] || "") : "";
            }

            function setMediaStatus(message, isError) {
                if (!mediaStatus) { return; }
                mediaStatus.textContent = message || "";
                mediaStatus.classList.toggle("is-error", !!isError);
                if (mediaRetry) { mediaRetry.hidden = !isError; }
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

                    var preview = document.createElement(itemType === "video" ? "div" : "a");
                    preview.className = "media-suggest-preview";
                    if (itemType === "video") {
                        var clip = document.createElement("video");
                        clip.src = fullUrl;
                        clip.poster = previewUrl;
                        clip.controls = true;
                        clip.preload = "none";
                        clip.setAttribute("aria-label", title);
                        preview.appendChild(clip);
                    } else {
                        preview.href = sourceUrl || fullUrl;
                        preview.target = "_blank";
                        preview.rel = "noopener";
                        var image = document.createElement("img");
                        image.src = previewUrl;
                        image.alt = title;
                        image.loading = "lazy";
                        preview.appendChild(image);
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
                    var choose = document.createElement("button");
                    choose.type = "button";
                    choose.className = "btn-primary btn-sm";
                    choose.textContent = section.dataset.selectLabel || "Select for preview";
                    choose.addEventListener("click", function () {
                        editForm.dispatchEvent(new CustomEvent("vocanova-media-selected", {
                            bubbles: true, detail: { type: itemType, url: fullUrl, thumbnail: previewUrl, title: title }
                        }));
                    });
                    actions.appendChild(choose);
                    var source = document.createElement("a");
                    source.href = sourceUrl || fullUrl;
                    source.target = "_blank";
                    source.rel = "noopener";
                    source.className = "btn-secondary btn-sm";
                    source.textContent = "Pexels";
                    actions.appendChild(source);
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
                if (!mediaRetry || mediaRetry.disabled || !mediaResults) { return; }
                var params = new URLSearchParams();
                params.set("type", mediaType);
                params.set("limit", "8");
                mediaRetry.disabled = true;
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
                    .finally(function () { mediaRetry.disabled = false; });
            }

            loadMediaSuggestions();
            if (mediaRetry) {
                mediaRetry.addEventListener("click", loadMediaSuggestions);
            }

        });
    }

    if (editForm) {
        editForm.addEventListener("play", function (event) {
            editForm.querySelectorAll("audio").forEach(function (audio) {
                if (audio !== event.target) { audio.pause(); }
            });
            if (audioEl && audioEl !== event.target) { audioEl.pause(); }
        }, true);
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
    document.addEventListener("vocanova-video-play", function () { if (audioEl) { audioEl.pause(); } });
    document.querySelectorAll(".audio-btn[data-audio]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var url = btn.getAttribute("data-audio");
            if (!url) {
                return;
            }
            if (!audioEl) {
                audioEl = new Audio();
            }
            stopAudio();
            document.dispatchEvent(new Event("vocanova-audio-play"));
            audioEl.src = url;
            audioEl.play().catch(function () { showToast(editForm.dataset.msgRequestFailed, false); });
        });
    });
})();
