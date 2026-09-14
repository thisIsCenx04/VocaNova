// Download only the selected public Pexels asset; existing multipart APIs remain authoritative.
window.downloadSuggestedMedia = async function (url, type) {
    var uri = new URL(url);
    if (uri.protocol !== "https:" || !["images.pexels.com", "videos.pexels.com"].includes(uri.hostname)) {
        throw new Error("Invalid media source.");
    }
    var maximum = (type === "video" ? 20 : 5) * 1024 * 1024;
    var abort = new AbortController();
    var timer = setTimeout(function () { abort.abort(); }, 60000);
    try {
        var response = await fetch(uri.href, { credentials: "omit", signal: abort.signal });
        if (!response.ok || !response.body) { throw new Error("Media download failed."); }
        if (Number(response.headers.get("Content-Length")) > maximum) { throw new Error("Media is too large."); }
        var mime = (response.headers.get("Content-Type") || "").split(";")[0].trim().toLowerCase();
        var extensions = type === "video" ? { "video/mp4": "mp4" } : { "image/jpeg": "jpg", "image/png": "png", "image/webp": "webp" };
        if (!extensions[mime]) { throw new Error("Unsupported media format."); }
        var reader = response.body.getReader();
        var chunks = [];
        var length = 0;
        while (true) {
            var part = await reader.read();
            if (part.done) { break; }
            length += part.value.byteLength;
            if (length > maximum) { throw new Error("Media is too large."); }
            chunks.push(part.value);
        }
        if (!length) { throw new Error("Empty media."); }
        return new File(chunks, "pexels." + extensions[mime], { type: mime });
    } finally { clearTimeout(timer); abort.abort(); }
};
