using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F056 — Dashboard Overview: 4 stat card, line chart sessions/ngày, pie chart phân bố mastery.
// Dữ liệu nạp qua AJAX (OverviewData) và tự refresh mỗi 5 phút phía client.
public sealed class DashboardController : Controller
{
    private const int SessionsTrendDays = 7;

    private readonly IVocaNovaApiClient _apiClient;

    public DashboardController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Trả dữ liệu tổng hợp cho trang Overview (dùng cho lần nạp đầu + auto-refresh setInterval).
    [HttpGet("/dashboard/overview-data")]
    public async Task<IActionResult> OverviewData(CancellationToken cancellationToken)
    {
        var stats = await _apiClient.GetDashboardStatsAsync(cancellationToken);
        var trend = await _apiClient.GetSessionsTrendAsync(SessionsTrendDays, cancellationToken);
        var mastery = await _apiClient.GetMasteryDistributionAsync(cancellationToken);

        return Json(new
        {
            stats = new
            {
                totalUsers = stats?.TotalUsers ?? 0,
                totalWords = stats?.TotalWords ?? 0,
                sessionsToday = stats?.SessionsToday ?? 0,
                avgAccuracy7d = stats?.AvgAccuracy7d ?? 0d,
            },
            sessionsTrend = new
            {
                labels = trend?.Points.Select(point => point.Date).ToArray() ?? Array.Empty<string>(),
                values = trend?.Points.Select(point => point.SessionCount).ToArray() ?? Array.Empty<int>(),
            },
            mastery = new
            {
                totalWordsInProgress = mastery?.TotalWordsInProgress ?? 0,
                labels = mastery?.Levels.Select(level => $"Level {level.Level}").ToArray() ?? Array.Empty<string>(),
                values = mastery?.Levels.Select(level => level.WordCount).ToArray() ?? Array.Empty<int>(),
            },
        });
    }
}
