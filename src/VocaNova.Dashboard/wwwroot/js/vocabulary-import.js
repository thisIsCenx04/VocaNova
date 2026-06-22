(function () {
    'use strict';

    var form = document.getElementById('csv-import-form');
    var input = document.getElementById('csv-file');
    var dropZone = document.getElementById('csv-drop-zone');
    var selectedFile = document.getElementById('selected-file');
    var selectedName = document.getElementById('selected-file-name');
    var selectedSize = document.getElementById('selected-file-size');
    var clearButton = document.getElementById('clear-csv-file');
    var submitButton = document.getElementById('import-submit');
    var feedback = document.getElementById('import-feedback');
    var results = document.getElementById('import-results');
    var config = document.getElementById('import-page-config');
    var pipelineSteps = document.querySelectorAll('.import-pipeline li');
    var lastErrors = [];

    if (!form || !input || !dropZone || !config) {
        return;
    }

    input.addEventListener('change', function () {
        updateSelectedFile(input.files && input.files[0]);
    });

    ['dragenter', 'dragover'].forEach(function (eventName) {
        dropZone.addEventListener(eventName, function (event) {
            event.preventDefault();
            dropZone.classList.add('is-dragging');
        });
    });

    ['dragleave', 'drop'].forEach(function (eventName) {
        dropZone.addEventListener(eventName, function (event) {
            event.preventDefault();
            dropZone.classList.remove('is-dragging');
        });
    });

    dropZone.addEventListener('drop', function (event) {
        var file = event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files[0];
        if (!file) {
            return;
        }

        var transfer = new DataTransfer();
        transfer.items.add(file);
        input.files = transfer.files;
        updateSelectedFile(file);
    });

    clearButton.addEventListener('click', clearFile);

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        var file = input.files && input.files[0];
        if (!validateFile(file)) {
            return;
        }

        setPipelineStep(1);
        setFeedback(config.dataset.working, 'working');
        submitButton.disabled = true;

        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(function (response) {
            return response.json().catch(function () { return {}; }).then(function (body) {
                if (!response.ok || body.success === false) {
                    throw new Error(body.message || config.dataset.failed);
                }
                return body;
            });
        }).then(function (body) {
            renderResult(body.data || {}, body.message || '');
            setPipelineStep(2);
            setFeedback('', 'success');
            clearFile(false);
            window.vnToast(body.message, 'ok');
        }).catch(function (error) {
            setPipelineStep(0);
            setFeedback(error.message || config.dataset.failed, 'error');
            window.vnToast(error.message || config.dataset.failed, 'err');
        }).finally(function () {
            submitButton.disabled = !(input.files && input.files[0]);
        });
    });

    document.getElementById('download-import-errors').addEventListener('click', function () {
        if (!lastErrors.length) {
            return;
        }

        var rows = [['row', 'column', 'message']].concat(lastErrors.map(function (error) {
            return [error.row, error.column, error.message];
        }));
        var csv = '\uFEFF' + rows.map(function (row) {
            return row.map(csvCell).join(',');
        }).join('\r\n');
        var url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
        var link = document.createElement('a');
        link.href = url;
        link.download = config.dataset.errorFileName || 'vocabulary-import-errors.csv';
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    });

    function updateSelectedFile(file) {
        if (!validateFile(file)) {
            clearFile(false);
            return;
        }

        selectedName.textContent = file.name;
        selectedSize.textContent = formatBytes(file.size);
        selectedFile.hidden = false;
        submitButton.disabled = false;
        feedback.hidden = true;
    }

    function validateFile(file) {
        if (!file) {
            return false;
        }
        if (!file.name.toLowerCase().endsWith('.csv')) {
            setFeedback(config.dataset.csvOnly, 'error');
            return false;
        }
        if (file.size > Number(config.dataset.maxFileBytes)) {
            setFeedback(config.dataset.fileTooLarge, 'error');
            return false;
        }
        return true;
    }

    function clearFile(clearFeedback) {
        input.value = '';
        selectedFile.hidden = true;
        selectedName.textContent = '';
        selectedSize.textContent = '';
        submitButton.disabled = true;
        if (clearFeedback !== false) {
            feedback.hidden = true;
        }
    }

    function setFeedback(message, kind) {
        feedback.textContent = message || '';
        feedback.className = 'import-feedback is-' + kind;
        feedback.hidden = !message;
    }

    function setPipelineStep(activeIndex) {
        pipelineSteps.forEach(function (step, index) {
            step.classList.toggle('is-complete', index < activeIndex);
            step.classList.toggle('is-active', index === activeIndex);
        });
    }

    function renderResult(data, message) {
        lastErrors = Array.isArray(data.errors) ? data.errors : [];
        document.getElementById('stat-imported-words').textContent = data.importedWords || 0;
        document.getElementById('stat-imported-senses').textContent = data.importedSenses || 0;
        document.getElementById('stat-skipped').textContent = data.skipped || 0;
        document.getElementById('stat-errors').textContent = lastErrors.length;
        document.getElementById('import-results-message').textContent = message;

        var errorPanel = document.getElementById('import-errors-panel');
        var successState = document.getElementById('import-success-state');
        var tableBody = document.getElementById('import-error-rows');
        tableBody.replaceChildren();

        lastErrors.forEach(function (error) {
            var row = document.createElement('tr');
            [error.row, error.column, error.message].forEach(function (value) {
                var cell = document.createElement('td');
                cell.textContent = value == null ? '' : String(value);
                row.appendChild(cell);
            });
            tableBody.appendChild(row);
        });

        errorPanel.hidden = lastErrors.length === 0;
        successState.hidden = lastErrors.length !== 0;
        results.hidden = false;
        results.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    }

    function csvCell(value) {
        var text = value == null ? '' : String(value);
        return '"' + text.replace(/"/g, '""') + '"';
    }

    function formatBytes(bytes) {
        if (bytes < 1024) {
            return bytes + ' B';
        }
        if (bytes < 1024 * 1024) {
            return (bytes / 1024).toFixed(1) + ' KB';
        }
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }
})();
