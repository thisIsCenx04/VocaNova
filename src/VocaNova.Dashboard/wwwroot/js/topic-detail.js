(function () {
    'use strict';

    var form = document.getElementById('topic-add-word-form');
    var input = document.getElementById('topic-add-word-input');
    var wordId = document.getElementById('topic-add-word-id');
    var suggestions = document.getElementById('topic-add-word-suggestions');
    var error = document.getElementById('topic-add-word-error');
    var timer;
    var request;

    if (!form || !input || !wordId || !suggestions || !error) return;

    function close() {
        suggestions.hidden = true;
        suggestions.replaceChildren();
    }

    input.addEventListener('input', function () {
        wordId.value = '';
        error.textContent = '';
        window.clearTimeout(timer);
        var query = input.value.trim();
        if (!query) return close();

        timer = window.setTimeout(function () {
            if (request) request.abort();
            request = new AbortController();
            fetch('/topics/word-suggestions?q=' + encodeURIComponent(query), { signal: request.signal })
                .then(function (response) { return response.ok ? response.json() : []; })
                .then(function (items) {
                    suggestions.replaceChildren();
                    items.forEach(function (item) {
                        var option = document.createElement('button');
                        option.type = 'button';
                        option.className = 'topic-word-suggestion';
                        option.dataset.id = item.wordId;
                        option.dataset.word = item.word;
                        option.innerHTML = '<strong></strong><span></span>';
                        option.querySelector('strong').textContent = item.word;
                        option.querySelector('span').textContent = item.primaryMeaning || '';
                        suggestions.appendChild(option);
                    });
                    suggestions.hidden = items.length === 0;
                })
                .catch(function (reason) { if (reason.name !== 'AbortError') close(); });
        }, 200);
    });

    suggestions.addEventListener('click', function (event) {
        var option = event.target.closest('.topic-word-suggestion');
        if (!option) return;
        input.value = option.dataset.word;
        wordId.value = option.dataset.id;
        close();
    });

    form.addEventListener('submit', function (event) {
        if (wordId.value) return;
        event.preventDefault();
        error.textContent = 'Please select an existing vocabulary word from the suggestions.';
        input.focus();
    });
}());
