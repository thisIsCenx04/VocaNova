(function () {
    'use strict';

    var input = document.getElementById('topic-keyword-input');
    var addButton = document.getElementById('topic-keyword-add');
    var list = document.getElementById('topic-keyword-list');
    var empty = document.getElementById('topic-keyword-empty');
    var suggestions = document.getElementById('topic-word-suggestions');
    var keywordError = document.getElementById('topic-keyword-error');
    var searchTimer;
    var searchRequest;
    var selectedWordId = null;

    if (!input || !addButton || !list || !empty || !suggestions || !keywordError) return;

    function setKeywordError(message) {
        keywordError.textContent = message || '';
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
    }

    function closeSuggestions() {
        suggestions.hidden = true;
        suggestions.replaceChildren();
        input.setAttribute('aria-expanded', 'false');
    }

    function showSuggestions(words) {
        suggestions.replaceChildren();
        if (!words.length) {
            closeSuggestions();
            return;
        }

        words.forEach(function (item) {
            var option = document.createElement('button');
            option.type = 'button';
            option.className = 'topic-word-suggestion';
            option.setAttribute('role', 'option');
            option.dataset.word = item.word;
            option.dataset.wordId = item.wordId;

            var word = document.createElement('strong');
            word.textContent = item.word;
            option.appendChild(word);

            if (item.primaryMeaning) {
                var meaning = document.createElement('span');
                meaning.textContent = item.primaryMeaning;
                option.appendChild(meaning);
            }

            suggestions.appendChild(option);
        });

        suggestions.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    }

    function findWords() {
        var query = input.value.trim();
        if (!query) {
            closeSuggestions();
            return;
        }

        if (searchRequest) searchRequest.abort();
        searchRequest = new AbortController();
        fetch('/topics/word-suggestions?q=' + encodeURIComponent(query), {
            signal: searchRequest.signal,
            headers: { 'Accept': 'application/json' }
        })
            .then(function (response) { return response.ok ? response.json() : []; })
            .then(showSuggestions)
            .catch(function (error) {
                if (error.name !== 'AbortError') closeSuggestions();
            });
    }

    function refreshEmptyState() {
        empty.hidden = list.querySelectorAll('.topic-keyword-chip').length > 0;
    }

    function addKeyword() {
        var value = input.value.trim();
        if (!value) {
            setKeywordError('Please enter or select a vocabulary word.');
            input.focus();
            return;
        }
        if (!selectedWordId) {
            setKeywordError('Please select an existing vocabulary word from the suggestions.');
            input.focus();
            return;
        }

        var duplicate = Array.from(list.querySelectorAll('input[name="Keywords"]'))
            .some(function (item) { return item.value.toLocaleLowerCase() === value.toLocaleLowerCase(); });
        if (duplicate) {
            setKeywordError('This vocabulary has already been added.');
            input.focus();
            return;
        }

        var chip = document.createElement('span');
        chip.className = 'topic-keyword-chip';

        var label = document.createElement('span');
        label.textContent = value;

        var hidden = document.createElement('input');
        hidden.type = 'hidden';
        hidden.name = 'Keywords';
        hidden.value = value;

        var hiddenWordId = document.createElement('input');
        hiddenWordId.type = 'hidden';
        hiddenWordId.name = 'WordIds';
        hiddenWordId.value = selectedWordId;

        var remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'topic-keyword-remove';
        remove.setAttribute('aria-label', 'Remove keyword');
        remove.innerHTML = '&times;';

        chip.appendChild(label);
        chip.appendChild(hidden);
        chip.appendChild(hiddenWordId);
        chip.appendChild(remove);
        list.appendChild(chip);
        input.value = '';
        selectedWordId = null;
        setKeywordError('');
        closeSuggestions();
        input.focus();
        refreshEmptyState();
    }

    addButton.addEventListener('click', addKeyword);
    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' || event.key === ',') {
            event.preventDefault();
            addKeyword();
        }
    });
    input.addEventListener('input', function () {
        selectedWordId = null;
        setKeywordError('');
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(findWords, 200);
    });
    suggestions.addEventListener('click', function (event) {
        var option = event.target.closest('.topic-word-suggestion');
        if (!option) return;
        input.value = option.dataset.word;
        selectedWordId = option.dataset.wordId;
        addKeyword();
    });
    document.addEventListener('click', function (event) {
        if (!event.target.closest('.topic-keyword-autocomplete')) closeSuggestions();
    });
    list.addEventListener('click', function (event) {
        var removeButton = event.target.closest('.topic-keyword-remove');
        if (!removeButton) return;
        removeButton.closest('.topic-keyword-chip').remove();
        setKeywordError('');
        refreshEmptyState();
    });

    refreshEmptyState();
}());
