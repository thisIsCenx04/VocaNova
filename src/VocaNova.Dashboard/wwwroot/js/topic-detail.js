(function () {
    'use strict';

    var toggle = document.getElementById('topic-add-word-toggle');
    var cancel = document.getElementById('topic-add-word-cancel');
    var panel = document.getElementById('topic-add-word-panel');
    var form = document.getElementById('topic-add-word-form');
    var input = document.getElementById('topic-add-word-input');
    var wordId = document.getElementById('topic-add-word-id');
    var suggestions = document.getElementById('topic-add-word-suggestions');
    var error = document.getElementById('topic-add-word-error');
    var timer;
    var request;

    if (!toggle || !cancel || !panel || !form || !input || !wordId || !suggestions || !error) return;

    function closeSuggestions() {
        suggestions.hidden = true;
        suggestions.replaceChildren();
        input.setAttribute('aria-expanded', 'false');
    }

    function resetSearch() {
        window.clearTimeout(timer);
        if (request) request.abort();
        input.value = '';
        wordId.value = '';
        error.textContent = '';
        closeSuggestions();
    }

    function setPanel(open) {
        panel.hidden = !open;
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        toggle.classList.toggle('is-open', open);
        if (open) input.focus();
        else resetSearch();
    }

    toggle.addEventListener('click', function () {
        setPanel(panel.hidden);
    });

    cancel.addEventListener('click', function () {
        setPanel(false);
        toggle.focus();
    });

    input.addEventListener('input', function () {
        wordId.value = '';
        error.textContent = '';
        window.clearTimeout(timer);
        if (request) request.abort();
        closeSuggestions();

        var query = input.value.trim();
        if (!query) return;

        // Chỉ gọi API khi người dùng đã ngừng gõ đủ 3 giây.
        timer = window.setTimeout(function () {
            request = new AbortController();
            fetch('/topics/word-suggestions?q=' + encodeURIComponent(query), {
                signal: request.signal,
                headers: { 'Accept': 'application/json' }
            })
                .then(function (response) { return response.ok ? response.json() : []; })
                .then(function (items) {
                    if (input.value.trim() !== query) return;
                    suggestions.replaceChildren();
                    items.forEach(function (item) {
                        var option = document.createElement('button');
                        option.type = 'button';
                        option.className = 'topic-word-suggestion';
                        option.dataset.id = item.wordId;
                        option.dataset.word = item.word;
                        option.setAttribute('role', 'option');

                        var label = document.createElement('strong');
                        label.textContent = item.word;
                        option.appendChild(label);

                        var meaning = document.createElement('span');
                        meaning.textContent = item.primaryMeaning || '';
                        option.appendChild(meaning);
                        suggestions.appendChild(option);
                    });
                    suggestions.hidden = items.length === 0;
                    input.setAttribute('aria-expanded', items.length ? 'true' : 'false');
                })
                .catch(function (reason) {
                    if (reason.name !== 'AbortError') closeSuggestions();
                });
        }, 3000);
    });

    suggestions.addEventListener('click', function (event) {
        var option = event.target.closest('.topic-word-suggestion');
        if (!option) return;
        input.value = option.dataset.word;
        wordId.value = option.dataset.id;
        closeSuggestions();
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.topic-keyword-autocomplete')) closeSuggestions();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        if (!suggestions.hidden) closeSuggestions();
        else if (!panel.hidden) setPanel(false);
    });

    form.addEventListener('submit', function (event) {
        if (wordId.value) return;
        event.preventDefault();
        error.textContent = form.dataset.selectError || 'Please select an existing vocabulary word from the suggestions.';
        input.focus();
    });

    toggle.classList.toggle('is-open', !panel.hidden);
}());
