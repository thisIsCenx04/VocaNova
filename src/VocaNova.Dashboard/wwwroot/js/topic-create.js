(function () {
    'use strict';

    // ----- Modal chọn icon -----
    // Ô input vẫn là nguồn dữ liệu post lên; modal chỉ ghi giá trị vào đó.
    (function initIconPicker() {
        var picker = document.querySelector('[data-topic-icon-picker]');
        var modal = document.querySelector('[data-topic-icon-modal]');
        if (!picker || !modal) return;

        var trigger = picker.querySelector('[data-topic-icon-trigger]');
        var field = picker.querySelector('[data-topic-icon-input]');
        var preview = picker.querySelector('[data-topic-icon-preview]');
        var search = modal.querySelector('[data-topic-icon-search]');
        var scroll = modal.querySelector('[data-topic-icon-scroll]');
        var emptyNote = modal.querySelector('[data-topic-icon-empty]');
        var options = Array.from(modal.querySelectorAll('[data-topic-icon-option]'));
        var groups = Array.from(modal.querySelectorAll('[data-topic-icon-group]'));
        var tabs = Array.from(modal.querySelectorAll('[data-topic-icon-tab]'));
        if (!trigger || !field || !preview) return;

        function syncPreview() {
            var value = field.value.trim();
            preview.textContent = value || '🎨';
            options.forEach(function (option) {
                option.classList.toggle('is-selected', option.dataset.topicIconOption === value);
            });
        }

        function applyFilter() {
            var term = (search ? search.value : '').trim().toLocaleLowerCase();
            var matches = 0;

            options.forEach(function (option) {
                var hit = !term
                    || option.dataset.keywords.toLocaleLowerCase().indexOf(term) !== -1;
                option.classList.toggle('is-hidden', !hit);
                if (hit) matches++;
            });

            // Ẩn luôn tiêu đề nhóm không còn icon nào, tránh để lại nhãn trống.
            groups.forEach(function (group) {
                var visible = group.querySelectorAll('[data-topic-icon-option]:not(.is-hidden)').length;
                group.classList.toggle('is-hidden', visible === 0);
            });

            if (emptyNote) emptyNote.classList.toggle('is-hidden', matches > 0);
        }

        function openModal() {
            modal.hidden = false;
            document.body.classList.add('crud-modal-open');
            if (search) {
                search.value = '';
                applyFilter();
                search.focus();
            }
        }

        function closeModal() {
            modal.hidden = true;
            document.body.classList.remove('crud-modal-open');
            trigger.focus();
        }

        trigger.addEventListener('click', openModal);

        modal.addEventListener('click', function (event) {
            // Bấm ra vùng nền tối cũng đóng, giống các modal khác của dashboard.
            if (event.target === modal) {
                closeModal();
                return;
            }

            if (event.target.closest('[data-topic-icon-close]')) {
                closeModal();
                return;
            }

            if (event.target.closest('[data-topic-icon-clear]')) {
                field.value = '';
                syncPreview();
                closeModal();
                return;
            }

            var tab = event.target.closest('[data-topic-icon-tab]');
            if (tab) {
                var target = modal.querySelector('[data-topic-icon-group="' + tab.dataset.topicIconTab + '"]');
                if (target && scroll) {
                    scroll.scrollTop = target.offsetTop - scroll.offsetTop;
                }
                tabs.forEach(function (item) { item.classList.toggle('is-active', item === tab); });
                return;
            }

            var option = event.target.closest('[data-topic-icon-option]');
            if (option) {
                field.value = option.dataset.topicIconOption;
                syncPreview();
                closeModal();
            }
        });

        if (search) {
            search.addEventListener('input', applyFilter);
            // Modal nằm trong form, nên Enter ở ô tìm kiếm sẽ submit topic nếu không chặn.
            search.addEventListener('keydown', function (event) {
                if (event.key === 'Enter') event.preventDefault();
            });
        }

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !modal.hidden) closeModal();
        });

        syncPreview();
    }());

    // ----- Danh sách từ vựng của topic -----
    var input = document.getElementById('topic-keyword-input');
    var addButton = document.getElementById('topic-keyword-add');
    var list = document.getElementById('topic-keyword-list');
    var empty = document.getElementById('topic-keyword-empty');
    var suggestions = document.getElementById('topic-word-suggestions');
    var keywordError = document.getElementById('topic-keyword-error');
    var searchTimer;
    var searchRequest;
    var selected = null;

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
            // Giữ nguyên payload để dựng đủ các cột của dòng mới.
            option.dataset.payload = JSON.stringify(item);

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
        empty.classList.toggle('is-hidden', list.querySelectorAll('.topic-keyword-row').length > 0);
    }

    function textCell(value) {
        var cell = document.createElement('td');
        cell.textContent = value ? value : '—';
        return cell;
    }

    function buildRow(word) {
        var row = document.createElement('tr');
        row.className = 'topic-keyword-row';

        var wordCell = document.createElement('td');
        var label = document.createElement(word.wordId ? 'a' : 'span');
        label.className = 'vocab-word';
        label.textContent = word.word;
        if (word.wordId) label.href = '/vocabulary/' + word.wordId;
        wordCell.appendChild(label);

        var keywordField = document.createElement('input');
        keywordField.type = 'hidden';
        keywordField.name = 'Keywords';
        keywordField.value = word.word;
        wordCell.appendChild(keywordField);

        var wordIdField = document.createElement('input');
        wordIdField.type = 'hidden';
        wordIdField.name = 'WordIds';
        wordIdField.value = word.wordId || 0;
        wordCell.appendChild(wordIdField);

        row.appendChild(wordCell);
        row.appendChild(textCell(word.wordType));
        row.appendChild(textCell(word.cefr));
        row.appendChild(textCell(word.phonetic));

        var statusCell = document.createElement('td');
        if (word.status) {
            var badge = document.createElement('span');
            badge.className = word.status === 'deleted' ? 'badge badge-danger' : 'badge badge-success';
            badge.textContent = word.status;
            statusCell.appendChild(badge);
        } else {
            statusCell.textContent = '—';
        }
        row.appendChild(statusCell);

        var actionCell = document.createElement('td');
        actionCell.className = 'col-actions';
        var icons = document.createElement('div');
        icons.className = 'action-icons';
        var remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'icon-link topic-keyword-remove danger';
        remove.title = 'Remove from topic';
        remove.setAttribute('aria-label', 'Remove from topic');
        remove.innerHTML = '<svg viewBox="0 0 24 24"><path d="M9 3h6l1 2h4v2H4V5h4l1-2Zm-3 6h12l-1 12H7L6 9Z"/></svg>';
        icons.appendChild(remove);
        actionCell.appendChild(icons);
        row.appendChild(actionCell);

        return row;
    }

    function addKeyword() {
        var value = input.value.trim();
        if (!value) {
            setKeywordError('Please enter or select a vocabulary word.');
            input.focus();
            return;
        }
        if (!selected || !selected.wordId) {
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

        // Chèn trước dòng "chưa có từ nào" để dòng đó luôn nằm cuối bảng.
        list.insertBefore(buildRow(selected), empty);
        input.value = '';
        selected = null;
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
        selected = null;
        setKeywordError('');
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(findWords, 200);
    });
    suggestions.addEventListener('click', function (event) {
        var option = event.target.closest('.topic-word-suggestion');
        if (!option) return;
        selected = JSON.parse(option.dataset.payload);
        input.value = selected.word;
        addKeyword();
    });
    document.addEventListener('click', function (event) {
        if (!event.target.closest('.topic-keyword-autocomplete')) closeSuggestions();
    });
    list.addEventListener('click', function (event) {
        var removeButton = event.target.closest('.topic-keyword-remove');
        if (!removeButton) return;
        removeButton.closest('.topic-keyword-row').remove();
        setKeywordError('');
        refreshEmptyState();
    });

    refreshEmptyState();
}());
