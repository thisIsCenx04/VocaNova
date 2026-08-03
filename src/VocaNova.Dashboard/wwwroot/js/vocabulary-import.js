// F059 — CSV import: drag-drop, AJAX upload, render result + errors, download errors as CSV.
(function () {
    'use strict';

    var form = document.getElementById('import-form');
    if (!form) {
        return;
    }

    var tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenField ? tokenField.value : '';
    var dropZone = document.getElementById('drop-zone');
    var fileInput = document.getElementById('csv-file');
    var dropFile = document.getElementById('drop-file');
    var submitBtn = document.getElementById('import-submit');
    var resultPanel = document.getElementById('import-result');
    var downloadBtn = document.getElementById('download-errors');
    var lastErrors = [];

    function showFileName() {
        dropFile.textContent = fileInput.files.length ? fileInput.files[0].name : '';
    }

    fileInput.addEventListener('change', showFileName);

    ['dragenter', 'dragover'].forEach(function (evt) {
        dropZone.addEventListener(evt, function (e) {
            e.preventDefault();
            dropZone.classList.add('drag-over');
        });
    });
    ['dragleave', 'drop'].forEach(function (evt) {
        dropZone.addEventListener(evt, function (e) {
            e.preventDefault();
            dropZone.classList.remove('drag-over');
        });
    });
    dropZone.addEventListener('drop', function (e) {
        if (e.dataTransfer && e.dataTransfer.files.length) {
            fileInput.files = e.dataTransfer.files;
            showFileName();
        }
    });

    function setText(id, value) {
        var el = document.getElementById(id);
        if (el) { el.textContent = value; }
    }

    function renderResult(result) {
        setText('r-words', result.imported_words);
        setText('r-updated', result.updated_words || 0);
        setText('r-senses', result.imported_senses);
        setText('r-topics', result.imported_topics || 0);
        setText('r-examples', result.imported_examples || 0);
        setText('r-skipped', result.skipped);
        var errors = result.errors || [];
        lastErrors = errors;
        setText('r-errors', errors.length);

        var block = document.getElementById('errors-block');
        var body = document.getElementById('errors-body');
        body.innerHTML = '';
        if (errors.length) {
            errors.forEach(function (err) {
                var tr = document.createElement('tr');
                tr.className = 'error-row';
                tr.innerHTML = '<td>' + err.row + '</td><td>' + escapeHtml(err.column) + '</td><td>' + escapeHtml(err.message) + '</td>';
                body.appendChild(tr);
            });
            block.hidden = false;
            downloadBtn.hidden = false;
        } else {
            block.hidden = true;
            downloadBtn.hidden = true;
        }
        resultPanel.hidden = false;
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function csvCell(s) {
        var v = String(s == null ? '' : s);
        return '"' + v.replace(/"/g, '""') + '"';
    }

    // Localized labels/messages come from data-* (rendered by @T).
    var L = form.dataset;
    form.addEventListener('submit', function (e) {
        e.preventDefault();
        if (!fileInput.files.length) {
            window.VocaNovaDashboard.notify(L.msgChoose || 'Please choose a CSV file.', false);
            return;
        }
        var data = new FormData();
        data.append('file', fileInput.files[0]);

        submitBtn.disabled = true;
        submitBtn.textContent = submitBtn.dataset.labelBusy || 'Importing...';

        fetch('/vocabulary/import', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: data
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (data.success && data.result) {
                    renderResult(data.result);
                } else {
                    window.VocaNovaDashboard.notify(data.message || L.msgFailed || 'Import failed.', false);
                }
            })
            .catch(function () { window.VocaNovaDashboard.notify(L.msgRequestFailed || 'Import request failed.', false); })
            .finally(function () {
                submitBtn.disabled = false;
                submitBtn.textContent = submitBtn.dataset.labelIdle || 'Upload & import';
            });
    });

    downloadBtn.addEventListener('click', function () {
        if (!lastErrors.length) { return; }
        var rows = ['row,column,message'];
        lastErrors.forEach(function (err) {
            rows.push([csvCell(err.row), csvCell(err.column), csvCell(err.message)].join(','));
        });
        var blob = new Blob(['﻿' + rows.join('\n')], { type: 'text/csv;charset=utf-8;' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = 'import-errors.csv';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    });
})();
