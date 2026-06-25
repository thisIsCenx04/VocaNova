// F062 — Statistics: activity (sessions + accuracy) by granularity, demographics bar charts.
(function () {
    'use strict';

    var ACCENT = '#0f766e';
    var BAR_COLORS = ['#0f766e', '#14b8a6', '#5eead4', '#f97316', '#eab308', '#6366f1', '#ec4899', '#22c55e'];

    var sessionsChart = null;
    var accuracyChart = null;

    function lineChart(canvasId, label, fillRgba) {
        var ctx = document.getElementById(canvasId);
        if (!ctx) { return null; }
        return new Chart(ctx, {
            type: 'line',
            data: { labels: [], datasets: [{ label: label, data: [], borderColor: ACCENT, backgroundColor: fillRgba, fill: true, tension: 0.3, pointRadius: 2 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    function updateLine(chart, labels, values) {
        if (!chart) { return; }
        chart.data.labels = labels;
        chart.data.datasets[0].data = values;
        chart.update();
    }

    function loadActivity(granularity) {
        fetch('/statistics/activity-trend?granularity=' + encodeURIComponent(granularity), { headers: { 'Accept': 'application/json' } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.success) { return; }
                updateLine(sessionsChart, data.labels, data.sessions);
                updateLine(accuracyChart, data.labels, data.accuracy);
            })
            .catch(function (err) { if (window.console) { console.error('activity-trend load failed', err); } });
    }

    function barChart(canvasId, payload) {
        var ctx = document.getElementById(canvasId);
        if (!ctx || !payload) { return; }
        var colors = (payload.labels || []).map(function (_, i) { return BAR_COLORS[i % BAR_COLORS.length]; });
        new Chart(ctx, {
            type: 'bar',
            data: { labels: payload.labels, datasets: [{ label: 'Users', data: payload.values, backgroundColor: colors }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }

    // ----- init -----
    sessionsChart = lineChart('sessions-chart', 'Sessions', 'rgba(15, 118, 110, 0.12)');
    accuracyChart = lineChart('accuracy-chart', 'Accuracy %', 'rgba(20, 184, 166, 0.12)');

    var select = document.getElementById('granularity');
    if (select) {
        select.addEventListener('change', function () { loadActivity(select.value); });
        loadActivity(select.value);
    } else {
        loadActivity('daily');
    }

    var demoEl = document.getElementById('demographics-data');
    if (demoEl) {
        try {
            var demo = JSON.parse(demoEl.textContent || '{}');
            barChart('age-chart', demo.age);
            barChart('occupation-chart', demo.occupation);
            barChart('education-chart', demo.education);
        } catch (e) {
            if (window.console) { console.error('demographics parse failed', e); }
        }
    }
})();
