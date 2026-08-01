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
        var codeLabel = picker.querySelector('[data-topic-icon-code]');
        var search = modal.querySelector('[data-topic-icon-search]');
        var scroll = modal.querySelector('[data-topic-icon-scroll]');
        var emptyNote = modal.querySelector('[data-topic-icon-empty]');
        var customInput = modal.querySelector('[data-topic-icon-custom]');
        var customError = modal.querySelector('[data-topic-icon-error]');
        var options = Array.from(modal.querySelectorAll('[data-topic-icon-option]'));
        var groups = Array.from(modal.querySelectorAll('[data-topic-icon-group]'));
        var tabs = Array.from(modal.querySelectorAll('[data-topic-icon-tab]'));
        if (!trigger || !field || !preview) return;

        function syncPreview() {
            var value = field.value.trim() || 'bi bi-book';
            preview.className = 'topic-icon-preview ' + value;
            if (codeLabel) codeLabel.textContent = value;
            if (customInput) customInput.value = value;
            options.forEach(function (option) {
                option.classList.toggle('is-selected', option.dataset.topicIconOption === value);
            });
        }

        function setCustomError(message) {
            if (customError) customError.textContent = message || '';
            if (customInput) customInput.setAttribute('aria-invalid', message ? 'true' : 'false');
        }

        function applyCustomIcon() {
            var value = customInput ? customInput.value.trim().replace(/\s+/g, ' ') : '';
            if (!/^bi bi-[a-z0-9-]+$/.test(value) || value.length > 20) {
                setCustomError('Please enter a valid Bootstrap icon class, for example: bi bi-book.');
                if (customInput) customInput.focus();
                return;
            }

            field.value = value;
            setCustomError('');
            syncPreview();
            closeModal();
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
            setCustomError('');
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
                field.value = 'bi bi-book';
                syncPreview();
                closeModal();
                return;
            }

            if (event.target.closest('[data-topic-icon-apply]')) {
                applyCustomIcon();
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

        if (customInput) {
            customInput.addEventListener('input', function () { setCustomError(''); });
            customInput.addEventListener('keydown', function (event) {
                if (event.key !== 'Enter') return;
                event.preventDefault();
                applyCustomIcon();
            });
        }

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !modal.hidden) closeModal();
        });

        syncPreview();
    }());

    // ----- Danh sách từ vựng của topic -----
    var toggleButton = document.getElementById('topic-keyword-toggle');
    var entry = document.getElementById('topic-keyword-entry');
    var input = document.getElementById('topic-keyword-input');
    var addButton = document.getElementById('topic-keyword-add');
    var cancelButton = document.getElementById('topic-keyword-cancel');
    var state = document.getElementById('topic-word-state');
    var list = document.getElementById('topic-keyword-list');
    var empty = document.getElementById('topic-keyword-empty');
    var suggestions = document.getElementById('topic-word-suggestions');
    var keywordError = document.getElementById('topic-keyword-error');
    var clientFilter = document.querySelector('[data-topic-client-filter]');
    var filterQuery = clientFilter && clientFilter.querySelector('[data-topic-filter-query]');
    var filterCefr = clientFilter && clientFilter.querySelector('[data-topic-filter-cefr]');
    var filterType = clientFilter && clientFilter.querySelector('[data-topic-filter-type]');
    var filterStatus = clientFilter && clientFilter.querySelector('[data-topic-filter-status]');
    var filterApply = clientFilter && clientFilter.querySelector('[data-topic-filter-apply]');
    var filterReset = clientFilter && clientFilter.querySelector('[data-topic-filter-reset]');
    var clientPageSize = document.querySelector('[data-topic-client-page-size]');
    var clientPager = document.querySelector('[data-topic-client-pager]');
    var currentClientPage = 1;
    var searchTimer;
    var searchRequest;
    var selected = null;

    if (!toggleButton || !entry || !input || !addButton || !cancelButton
        || !list || !empty || !suggestions || !keywordError) return;

    function setKeywordError(message) {
        keywordError.textContent = message || '';
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
    }

    function closeSuggestions() {
        suggestions.hidden = true;
        suggestions.replaceChildren();
        input.setAttribute('aria-expanded', 'false');
    }

    function resetWordSearch() {
        window.clearTimeout(searchTimer);
        if (searchRequest) searchRequest.abort();
        input.value = '';
        selected = null;
        setKeywordError('');
        closeSuggestions();
    }

    function setEntryOpen(open) {
        entry.hidden = !open;
        toggleButton.setAttribute('aria-expanded', open ? 'true' : 'false');
        toggleButton.classList.toggle('is-open', open);
        if (open) input.focus();
        else resetWordSearch();
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
        if (clientFilter) {
            renderClientList(false);
            return;
        }
        empty.classList.toggle('is-hidden', list.querySelectorAll('.topic-keyword-row').length > 0);
    }

    function clientRows() {
        return Array.from(list.querySelectorAll('.topic-keyword-row'));
    }

    function matchesClientFilter(row) {
        var query = filterQuery.value.trim().toLocaleLowerCase();
        var cefr = filterCefr.value.toLocaleLowerCase();
        var type = filterType.value.toLocaleLowerCase();
        var status = filterStatus.value.toLocaleLowerCase();
        return (!query || (row.dataset.word || '').toLocaleLowerCase().indexOf(query) !== -1)
            && (!cefr || (row.dataset.cefr || '').toLocaleLowerCase() === cefr)
            && (!type || (row.dataset.wordType || '').toLocaleLowerCase() === type)
            && (!status || (row.dataset.status || '').toLocaleLowerCase() === status);
    }

    function pagerButton(label, page, disabled, active) {
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'pager-link' + (disabled ? ' disabled' : '') + (active ? ' active' : '');
        button.textContent = label;
        button.disabled = disabled;
        if (!disabled && !active) {
            button.addEventListener('click', function () {
                currentClientPage = page;
                renderClientList(false);
            });
        }
        return button;
    }

    function renderClientPager(totalPages) {
        if (!clientPager) return;
        clientPager.replaceChildren();
        if (totalPages <= 1) return;

        var previousLabel = '‹ ' + (clientPager.dataset.prev || 'Prev');
        var nextLabel = (clientPager.dataset.next || 'Next') + ' ›';
        clientPager.appendChild(pagerButton(previousLabel, currentClientPage - 1, currentClientPage === 1, false));

        var start = Math.max(1, currentClientPage - 2);
        var end = Math.min(totalPages, start + 4);
        start = Math.max(1, end - 4);
        for (var page = start; page <= end; page++) {
            clientPager.appendChild(pagerButton(String(page), page, false, page === currentClientPage));
        }
        clientPager.appendChild(pagerButton(nextLabel, currentClientPage + 1, currentClientPage === totalPages, false));
    }

    function renderClientList(resetPage) {
        if (!clientFilter || !filterQuery || !filterCefr || !filterType || !filterStatus) return;
        if (resetPage) currentClientPage = 1;
        var matches = clientRows().filter(matchesClientFilter);
        var pageSize = clientPageSize ? Number(clientPageSize.value) || 10 : 10;
        var totalPages = Math.max(1, Math.ceil(matches.length / pageSize));
        currentClientPage = Math.min(currentClientPage, totalPages);
        var visible = new Set(matches.slice((currentClientPage - 1) * pageSize, currentClientPage * pageSize));

        clientRows().forEach(function (row) { row.hidden = !visible.has(row); });
        empty.classList.toggle('is-hidden', matches.length > 0);
        renderClientPager(totalPages);
    }

    function textCell(value) {
        var cell = document.createElement('td');
        cell.textContent = value ? value : '—';
        return cell;
    }

    function buildRow(word) {
        var row = document.createElement('tr');
        row.className = 'topic-keyword-row';
        row.dataset.wordId = word.wordId || 0;
        row.dataset.word = word.word || '';
        row.dataset.wordType = word.wordType || '';
        row.dataset.cefr = word.cefr || '';
        row.dataset.status = word.status || '';

        var wordCell = document.createElement('td');
        var label = document.createElement(word.wordId ? 'a' : 'span');
        label.className = 'vocab-word';
        label.textContent = word.word;
        if (word.wordId) label.href = '/vocabulary/' + word.wordId;
        wordCell.appendChild(label);

        // Create vẫn lưu state ngay trong dòng; Edit dùng kho state riêng để giữ cả các trang khác.
        if (!state) {
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
        }

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

    function appendState(word) {
        if (!state) return;
        var item = document.createElement('span');
        item.dataset.topicWordState = '';
        item.dataset.wordId = word.wordId;

        var keywordField = document.createElement('input');
        keywordField.type = 'hidden';
        keywordField.name = 'Keywords';
        keywordField.value = word.word;
        item.appendChild(keywordField);

        var wordIdField = document.createElement('input');
        wordIdField.type = 'hidden';
        wordIdField.name = 'WordIds';
        wordIdField.value = word.wordId;
        item.appendChild(wordIdField);
        state.appendChild(item);
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

        var stateRoot = state || list;
        var duplicate = Array.from(stateRoot.querySelectorAll('input[name="Keywords"]'))
            .some(function (item) { return item.value.toLocaleLowerCase() === value.toLocaleLowerCase(); });
        if (duplicate) {
            setKeywordError('This vocabulary has already been added.');
            input.focus();
            return;
        }

        // Chèn trước dòng "chưa có từ nào" để dòng đó luôn nằm cuối bảng.
        var addedRow = buildRow(selected);
        list.insertBefore(addedRow, empty);
        appendState(selected);
        if (clientFilter && matchesClientFilter(addedRow)) {
            var pageSize = clientPageSize ? Number(clientPageSize.value) || 10 : 10;
            currentClientPage = Math.max(1, Math.ceil(clientRows().filter(matchesClientFilter).length / pageSize));
        }
        input.value = '';
        selected = null;
        setKeywordError('');
        closeSuggestions();
        input.focus();
        refreshEmptyState();
    }

    addButton.addEventListener('click', addKeyword);
    if (filterApply) {
        filterApply.addEventListener('click', function () { renderClientList(true); });
    }
    if (filterReset) {
        filterReset.addEventListener('click', function () {
            filterQuery.value = '';
            filterCefr.value = '';
            filterType.value = '';
            filterStatus.value = '';
            renderClientList(true);
        });
    }
    if (filterQuery) {
        filterQuery.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter') return;
            event.preventDefault();
            renderClientList(true);
        });
    }
    if (clientPageSize) {
        clientPageSize.addEventListener('change', function () { renderClientList(true); });
    }
    toggleButton.addEventListener('click', function () {
        setEntryOpen(entry.hidden);
    });
    cancelButton.addEventListener('click', function () {
        setEntryOpen(false);
        toggleButton.focus();
    });
    input.addEventListener('keydown', function (event) {
        // Việc thêm từ phải được xác nhận bằng nút, không tự thêm bằng Enter.
        if (event.key === 'Enter') event.preventDefault();
    });
    input.addEventListener('input', function () {
        selected = null;
        setKeywordError('');
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(findWords, 3000);
    });
    suggestions.addEventListener('click', function (event) {
        var option = event.target.closest('.topic-word-suggestion');
        if (!option) return;
        selected = JSON.parse(option.dataset.payload);
        input.value = selected.word;
        setKeywordError('');
        closeSuggestions();
        addButton.focus();
    });
    document.addEventListener('click', function (event) {
        if (!event.target.closest('.topic-keyword-autocomplete')) closeSuggestions();
    });
    list.addEventListener('click', function (event) {
        var removeButton = event.target.closest('.topic-keyword-remove');
        if (!removeButton) return;
        var row = removeButton.closest('.topic-keyword-row');
        if (state) {
            var stateItem = Array.from(state.querySelectorAll('[data-topic-word-state]'))
                .find(function (item) { return item.dataset.wordId === row.dataset.wordId; });
            if (stateItem) stateItem.remove();
        }
        row.remove();
        setKeywordError('');
        refreshEmptyState();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape' || entry.hidden) return;
        if (!suggestions.hidden) closeSuggestions();
        else setEntryOpen(false);
    });

    refreshEmptyState();
}());
