using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Api.Stats;
using VocaNova.Dashboard.Models.Dashboard;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IVocaNovaApiClient _api;

    public DashboardController(IVocaNovaApiClient api)
    {
        _api = api;
    }

    [HttpGet("/dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stats = await _api.GetAsync<AdminDashboardStatsDto>("api/admin/stats/dashboard", cancellationToken);
        var learning = await _api.GetAsync<AdminLearningStatsDto>("api/admin/stats/learning", cancellationToken);

        var trendLoaded = learning.IsSuccess && learning.Data is not null;

        var model = new DashboardOverviewViewModel
        {
            Stats = stats.IsSuccess ? stats.Data : null,
            Trend = trendLoaded ? learning.Data!.AccuracyTrend : Array.Empty<AdminAccuracyTrendPointDto>(),
            TrendLoaded = trendLoaded,
        };

        return View(model);
    }

    // AJAX auto-refresh 5 phút (Index gọi qua fetch).
    [HttpGet("/dashboard/stats-json")]
    public async Task<IActionResult> StatsJson(CancellationToken cancellationToken)
    {
        var stats = await _api.GetAsync<AdminDashboardStatsDto>("api/admin/stats/dashboard", cancellationToken);
        var learning = await _api.GetAsync<AdminLearningStatsDto>("api/admin/stats/learning", cancellationToken);

        return Json(new
        {
            stats = stats.IsSuccess ? stats.Data : null,
            trend = learning.IsSuccess && learning.Data is not null
                ? learning.Data.AccuracyTrend
                : new List<AdminAccuracyTrendPointDto>(),
        });
    }
}
