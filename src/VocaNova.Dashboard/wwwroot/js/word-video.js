(function () {
    "use strict";
    var root = document.querySelector("[data-word-video]");
    if (!root) { return; }
    var input = root.querySelector("[data-video-file]");
    var pending = root.querySelector("[data-pending-video]");
    var preview = pending.querySelector("video");
    var current = root.querySelector("[data-current-video]");
    var saved = current.querySelector("video");
    var save = root.querySelector("[data-video-save]");
    var confirmation = root.querySelector("[data-video-confirm]");
    var message = root.querySelector("[data-video-message]");
    var objectUrl = null;
    var selected = null;
    var selectedUrl = null;
    var busy = false;
    var valid = false;
    var picker = root.querySelector("[data-video-picker]");
    if (picker) { picker.addEventListener("click", function () { if (!busy) { input.click(); } }); }

    function cancel() {
        preview.pause();
        preview.removeAttribute("src");
        preview.load();
        if (objectUrl) { URL.revokeObjectURL(objectUrl); }
        objectUrl = null;
        selected = null;
        selectedUrl = null;
        preview.removeAttribute("poster");
        valid = false;
        pending.hidden = true;
        input.value = "";
        save.disabled = true;
    }
    function setBusy(value) {
        busy = value;
        root.querySelectorAll("button,input").forEach(function (control) { control.disabled = value; });
        save.disabled = value || !valid;
        root.dispatchEvent(new CustomEvent("word-video-busy", { bubbles: true, detail: value }));
    }
    input.addEventListener("change", function () {
        if (busy) { return; }
        var file = input.files && input.files[0];
        cancel();
        message.textContent = "";
        if (!file) { return; }
        if (!/\.mp4$/i.test(file.name) || (file.type && file.type !== "video/mp4")) {
            message.textContent = root.dataset.invalid; return;
        }
        if (file.size > 20 * 1024 * 1024) { message.textContent = root.dataset.tooLarge; return; }
        selected = file;
        objectUrl = URL.createObjectURL(file);
        preview.src = objectUrl;
        pending.hidden = false;
    });
    document.addEventListener("vocanova-media-selected", function (event) {
        if (event.detail.type !== "video" || busy) { return; }
        cancel();
        selectedUrl = event.detail.url;
        preview.src = selectedUrl;
        preview.poster = event.detail.thumbnail || "";
        message.textContent = "";
        pending.hidden = false;
        pending.scrollIntoView({ behavior: "smooth", block: "center" });
    });
    preview.addEventListener("loadedmetadata", function () {
        valid = !!(selected || selectedUrl) && Number.isFinite(preview.duration) && preview.duration >= 5 && preview.duration <= 15
            && preview.videoWidth > 0 && preview.videoHeight > 0;
        save.disabled = busy || !valid;
        message.textContent = valid ? "" : root.dataset.duration;
    });
    preview.addEventListener("error", function () {
        if (!selected && !selectedUrl) { return; }
        valid = false; save.disabled = true; message.textContent = root.dataset.invalid;
    });
    root.querySelector("[data-video-cancel]").addEventListener("click", cancel);
    root.querySelector("[data-video-delete]").addEventListener("click", function () {
        confirmation.hidden = false;
        root.querySelector("[data-video-confirm-delete]").focus();
    });
    root.querySelector("[data-video-keep]").addEventListener("click", function () { confirmation.hidden = true; });

    async function submit(deleting) {
        if (busy || (!deleting && (!(selected || selectedUrl) || !valid))) { return; }
        var data = new FormData();
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) { data.append("__RequestVerificationToken", token.value); }
        setBusy(true);
        message.textContent = root.dataset.saving;
        try {
            if (!deleting) {
                var file = selected || await window.downloadSuggestedMedia(selectedUrl, "video");
                data.append("file", file, file.name);
            }
            var response = await fetch("/vocabulary/" + root.dataset.wordId + "/video" + (deleting ? "/delete" : ""), {
                method: "POST", body: data, credentials: "same-origin"
            });
            var result = await response.json();
            if (!response.ok || !result.success) { throw new Error(result.message || root.dataset.failed); }
            saved.pause();
            if (deleting) {
                saved.removeAttribute("src"); saved.removeAttribute("poster");
            } else {
                if (!result.video || !result.video.url) { throw new Error(root.dataset.failed); }
                saved.src = result.video.url;
                saved.poster = result.video.thumbnail_url;
                cancel();
            }
            saved.load();
            current.hidden = deleting;
            root.querySelector("[data-no-video]").hidden = !deleting;
            confirmation.hidden = true;
            message.textContent = result.message;
        } catch (error) { message.textContent = error.message || root.dataset.failed; }
        finally { setBusy(false); }
    }
    save.addEventListener("click", function () { submit(false); });
    root.querySelector("[data-video-confirm-delete]").addEventListener("click", function () { submit(true); });
    document.addEventListener("play", function (event) {
        if (!(event.target instanceof HTMLMediaElement)) { return; }
        document.querySelectorAll("audio,video").forEach(function (media) { if (media !== event.target) { media.pause(); } });
        if (event.target.tagName === "VIDEO") { document.dispatchEvent(new Event("vocanova-video-play")); }
    }, true);
    document.addEventListener("vocanova-audio-play", function () { saved.pause(); preview.pause(); });
    window.addEventListener("pagehide", function () { saved.pause(); cancel(); });
})();
