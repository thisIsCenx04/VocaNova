// Dashboard overview: activity line chart (sessions + accuracy) by granularity,
// mastery pie, "Most Difficult Words" table. Theme-aware; auto-refresh every 5 min.
(function () {
    'use strict';

    var REFRESH_MS = 5 * 60 * 1000;
    var VIOLET = '#6c5ce7';
    var CYAN = '#22b8d6';
    var MASTERY_COLORS = ['#c7c2f0', '#a9b8f0', '#7fd3e8', '#4fc3d6', '#34a9c4', '#6c5ce7'];

    var activityChart = null;
    var masteryChart = null;

    function themeColors() {
        var dark = document.documentElement.getAttribute('data-theme') === 'dark';
        return {
            grid: dark ? 'rgba(255,255,255,0.14)' : '#e1e4ee',
            tick: dark ? '#aab1c9' : '#6b7185',
        };
    }

    function readJson(id) {
        var el = document.getElementById(id);
        if (!el) { return null; }
        try { return JSON.parse(el.textContent || '{}'); } catch (e) { return null; }
    }

    function setText(id, value) {
        var el = document.getElementById(id);
        if (el) { el.textContent = value; }
    }

    function buildActivity(data) {
        var ctx = document.getElementById('activity-chart');
        if (!ctx) { return; }
        var c = themeColors();
        if (activityChart) {
            activityChart.data.labels = data.labels;
            activityChart.data.datasets[0].data = data.sessions;
            activityChart.data.datasets[1].data = data.accuracy;
            activityChart.update();
            return;
        }
        activityChart = new Chart(ctx, {
            data: {
                labels: data.labels,
                datasets: [
                    {
                        type: 'line', label: ctx.dataset.labelSessions || 'Sessions', data: data.sessions,
                        borderColor: VIOLET, backgroundColor: 'rgba(108,92,231,0.12)',
                        fill: true, tension: 0.4, pointRadius: 0, borderWidth: 2.5, yAxisID: 'y'
                    },
                    {
                        type: 'line', label: ctx.dataset.labelAccuracy || 'Accuracy %', data: data.accuracy,
                        borderColor: CYAN, backgroundColor: 'transparent',
                        fill: false, tension: 0.4, pointRadius: 0, borderWidth: 2, borderDash: [5, 4], yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { display: false }, ticks: { color: c.tick, maxRotation: 0, autoSkip: true, maxTicksLimit: 8 } },
                    y: { beginAtZero: true, grid: { color: c.grid }, ticks: { color: c.tick, precision: 0 } },
                    y1: { beginAtZero: true, max: 100, position: 'right', grid: { display: false }, ticks: { color: c.tick, callback: function (v) { return v + '%'; } } }
                }
            }
        });
    }

    function buildMastery(data) {
        var ctx = document.getElementById('mastery-chart');
        if (!ctx) { return; }
        setText('mastery-total', (data.total || 0).toLocaleString() + ' words in progress');
        var colors = (data.labels || []).map(function (_, i) { return MASTERY_COLORS[i % MASTERY_COLORS.length]; });
        if (masteryChart) {
            masteryChart.data.labels = data.labels;
            masteryChart.data.datasets[0].data = data.values;
            masteryChart.data.datasets[0].backgroundColor = colors;
            masteryChart.update();
            return;
        }
        masteryChart = new Chart(ctx, {
            type: 'doughnut',
            data: { labels: data.labels, datasets: [{ data: data.values, backgroundColor: colors, borderWidth: 2, borderColor: getComputedStyle(document.body).getPropertyValue('--surface') }] },
            options: { responsive: true, maintainAspectRatio: false, cutout: '62%', plugins: { legend: { position: 'right', labels: { color: themeColors().tick, boxWidth: 12, padding: 12 } } } }
        });
    }

    function sevBadge(sev) {
        if (sev === 'critical') { return 'badge badge-danger'; }
        if (sev === 'warning') { return 'badge badge-warning'; }
        return 'badge badge-success';
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function renderDifficult(list) {
        var body = document.getElementById('difficult-body');
        if (!body) { return; }
        if (!list || !list.length) {
            body.innerHTML = '<tr><td colspan="5" class="empty-row">' + (body.dataset.emptyMessage || 'No quiz data yet.') + '</td></tr>';
            return;
        }
        body.innerHTML = list.map(function (w) {
            return '<tr>' +
                '<td><span class="rank-pill">#' + w.rank + '</span></td>' +
                '<td class="word-text">' + escapeHtml(w.word) + '</td>' +
                '<td>' + w.attempts + '</td>' +
                '<td><div class="rate-cell"><div class="rate-bar"><div class="rate-fill sev-' + w.severity + '" style="width:' + w.failureRate + '%"></div></div>' +
                '<span class="rate-pct">' + w.failureRate + '%</span></div></td>' +
                '<td><span class="' + sevBadge(w.severity) + '">' + escapeHtml(w.statusLabel) + '</span></td>' +
                '</tr>';
        }).join('');
    }

    function updateCards(stats) {
        if (!stats) { return; }
        setText('stat-users', Number(stats.totalUsers).toLocaleString());
        setText('stat-words', Number(stats.totalWords).toLocaleString());
        setText('stat-sessions', Number(stats.sessionsToday).toLocaleString());
        setText('stat-accuracy', Number(stats.avgAccuracy7d).toFixed(1) + '%');
    }

    function loadData(granularity) {
        fetch('/dashboard/data?granularity=' + encodeURIComponent(granularity), { headers: { 'Accept': 'application/json' } })
            .then(function (res) { return res.json(); })
            .then(function (d) {
                updateCards(d.stats);
                buildActivity(d.activity);
                buildMastery(d.mastery);
                renderDifficult(d.difficultWords);
            })
            .catch(function (err) { if (window.console) { console.error('dashboard data load failed', err); } });
    }

    // Initial draw from server-embedded JSON (no flash).
    var activity0 = readJson('activity-data');
    var mastery0 = readJson('mastery-data');
    if (activity0) { buildActivity(activity0); }
    if (mastery0) { buildMastery(mastery0); }

    var select = document.getElementById('granularity');
    if (select && activity0 && activity0.granularity) { select.value = activity0.granularity; }
    if (select) {
        select.addEventListener('change', function () { loadData(select.value); });
    }

    // Auto-refresh (F056): cập nhật số liệu + chart mỗi 5 phút.
    window.setInterval(function () {
        loadData(select ? select.value : 'daily');
    }, REFRESH_MS);
})();
