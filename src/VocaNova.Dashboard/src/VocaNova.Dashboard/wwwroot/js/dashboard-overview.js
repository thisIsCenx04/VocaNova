// F056 — Dashboard Overview: nạp số liệu + vẽ chart, tự refresh mỗi 5 phút.
(function () {
    'use strict';

    var REFRESH_MS = 5 * 60 * 1000; // 5 phút
    var ACCENT = '#0f766e';
    // Thang màu theo mastery level 0..5 (nhạt → đậm).
    var MASTERY_COLORS = ['#e2e8f0', '#99f6e4', '#5eead4', '#2dd4bf', '#14b8a6', '#0f766e'];

    var sessionsChart = null;
    var masteryChart = null;

    function setText(id, value) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = value;
        }
    }

    function renderSessions(labels, values) {
        var ctx = document.getElementById('sessions-chart');
        if (!ctx) {
            return;
        }
        if (sessionsChart) {
            sessionsChart.data.labels = labels;
            sessionsChart.data.datasets[0].data = values;
            sessionsChart.update();
            return;
        }
        sessionsChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Sessions',
                    data: values,
                    borderColor: ACCENT,
                    backgroundColor: 'rgba(15, 118, 110, 0.12)',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 3,
                    pointBackgroundColor: ACCENT
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }

    function renderMastery(labels, values) {
        var ctx = document.getElementById('mastery-chart');
        if (!ctx) {
            return;
        }
        var colors = labels.map(function (_, i) {
            return MASTERY_COLORS[i % MASTERY_COLORS.length];
        });
        if (masteryChart) {
            masteryChart.data.labels = labels;
            masteryChart.data.datasets[0].data = values;
            masteryChart.data.datasets[0].backgroundColor = colors;
            masteryChart.update();
            return;
        }
        masteryChart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{ data: values, backgroundColor: colors, borderWidth: 1, borderColor: '#fff' }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'right' } }
            }
        });
    }

    function load() {
        fetch('/dashboard/overview-data', { headers: { 'Accept': 'application/json' } })
            .then(function (res) {
                if (!res.ok) { throw new Error('Request failed: ' + res.status); }
                return res.json();
            })
            .then(function (data) {
                setText('stat-users', data.stats.totalUsers.toLocaleString());
                setText('stat-words', data.stats.totalWords.toLocaleString());
                setText('stat-sessions', data.stats.sessionsToday.toLocaleString());
                setText('stat-accuracy', data.stats.avgAccuracy7d.toFixed(1) + '%');

                renderSessions(data.sessionsTrend.labels, data.sessionsTrend.values);

                var total = data.mastery.totalWordsInProgress;
                setText('mastery-total', total.toLocaleString() + ' words in progress');
                renderMastery(data.mastery.labels, data.mastery.values);
            })
            .catch(function (err) {
                if (window.console) {
                    console.error('Overview data load failed', err);
                }
            });
    }

    load();
    window.setInterval(load, REFRESH_MS);
})();
