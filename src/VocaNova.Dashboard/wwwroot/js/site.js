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
