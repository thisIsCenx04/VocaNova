(function () {
    'use strict';

    var config = document.getElementById('detail-page-config');
    if (!config) {
        return;
    }

    document.querySelectorAll('.audio-play-button').forEach(function (button) {
        button.addEventListener('click', function () {
            var player = document.getElementById(button.dataset.audioTarget);
            if (!player) {
                return;
            }

            document.querySelectorAll('audio').forEach(function (other) {
                if (other !== player) {
                    other.pause();
                }
            });
            player.currentTime = 0;
            player.play();
        });
    });

    document.querySelectorAll('.ajax-detail-form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            submitForm(form);
        });
    });

    function submitForm(form) {
        var submit = form.querySelector('[type="submit"]');
        var originalText = submit ? submit.textContent : '';
        if (submit) {
            submit.disabled = true;
            submit.textContent = config.dataset.working || originalText;
        }

        fetch(form.action, {
            method: form.method || 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (response) {
            return response.json().catch(function () { return {}; }).then(function (body) {
                if (!response.ok || body.success === false) {
                    throw new Error(body.message || config.dataset.error);
                }
                return body;
            });
        }).then(function (body) {
            window.vnToast(body.message, 'ok');
            window.setTimeout(function () { window.location.reload(); }, 350);
        }).catch(function (error) {
            window.vnToast(error.message || config.dataset.error, 'err');
            if (submit) {
                submit.disabled = false;
                submit.textContent = originalText;
            }
        });
    }
})();
