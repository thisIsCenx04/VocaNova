using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Data.Dtos.Stats;
using VocaNova.Dashboard.Models.Dashboard;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F056 + redesign: trang Dashboard overview — stat cards, line chart (granularity),
// mastery distribution, bảng "Most Difficult Words". Dữ liệu thật từ VocaNova.API.
public sealed class DashboardController : Controller
{
    private const int DifficultWordLimit = 8;

    private readonly IVocaNovaApiClient _apiClient;

    public DashboardController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stats = await _apiClient.GetDashboardStatsAsync(cancellationToken);
        var learning = await _apiClient.GetLearningStatsAsync(cancellationToken);
        var mastery = await _apiClient.GetMasteryDistributionAsync(cancellationToken);
        var activity = await _apiClient.GetActivityTrendAsync("daily", cancellationToken);

        var model = new DashboardOverviewViewModel
        {
            Stats = stats,
            DifficultWords = BuildDifficultWords(learning),
            Mastery = mastery,
            Activity = activity,
        };

        return View(model);
    }

    // Dùng cho dropdown granularity + auto-refresh 5 phút (JS).
    [HttpGet("/dashboard/data")]
    public async Task<IActionResult> Data(string granularity = "daily", CancellationToken cancellationToken = default)
    {
        var stats = await _apiClient.GetDashboardStatsAsync(cancellationToken);
        var learning = await _apiClient.GetLearningStatsAsync(cancellationToken);
        var mastery = await _apiClient.GetMasteryDistributionAsync(cancellationToken);
        var activity = await _apiClient.GetActivityTrendAsync(granularity, cancellationToken);

        return Json(new
        {
            stats = new
            {
                totalUsers = stats?.TotalUsers ?? 0,
                totalWords = stats?.TotalWords ?? 0,
                sessionsToday = stats?.SessionsToday ?? 0,
                avgAccuracy7d = stats?.AvgAccuracy7d ?? 0d,
            },
            activity = new
            {
                granularity = activity?.Granularity ?? granularity,
                labels = activity?.Points.Select(p => p.Period).ToArray() ?? Array.Empty<string>(),
                sessions = activity?.Points.Select(p => p.SessionsCount).ToArray() ?? Array.Empty<int>(),
                accuracy = activity?.Points.Select(p => p.Accuracy).ToArray() ?? Array.Empty<double>(),
            },
            mastery = new
            {
                labels = mastery?.Levels.Select(l => $"Level {l.Level}").ToArray() ?? Array.Empty<string>(),
                values = mastery?.Levels.Select(l => l.WordCount).ToArray() ?? Array.Empty<int>(),
                total = mastery?.TotalWordsInProgress ?? 0,
            },
            difficultWords = BuildDifficultWords(learning).Select(w => new
            {
                rank = w.Rank,
                word = w.Word,
                attempts = w.Attempts,
                failureRate = w.FailureRate,
                severity = w.Severity,
                statusLabel = w.StatusLabel,
            }),
        });
    }

    private static IReadOnlyList<DifficultWordRow> BuildDifficultWords(LearningStats? learning)
    {
        if (learning is null)
        {
            return Array.Empty<DifficultWordRow>();
        }

        return learning.TopWrongWords
            .Take(DifficultWordLimit)
            .Select((w, i) =>
            {
                var failure = (int)Math.Round(100 - w.Accuracy);
                if (failure < 0) failure = 0;
                if (failure > 100) failure = 100;

                var (severity, label) = failure >= 70
                    ? ("critical", "Critical")
                    : failure >= 50
                        ? ("warning", "Warning")
                        : ("improving", "Improving");

                return new DifficultWordRow(i + 1, w.Word, w.TotalCount, failure, severity, label);
            })
            .ToArray();
    }
}
