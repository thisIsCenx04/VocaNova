using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Statistics;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

// F062 — Statistics. Activity chart by granularity (AJAX), accuracy chart, top wrong words, demographics.
public sealed class StatisticsController : Controller
{
    private readonly IVocaNovaApiClient _apiClient;

    public StatisticsController(IVocaNovaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("/statistics")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var learning = await _apiClient.GetLearningStatsAsync(cancellationToken);
        var demographics = await _apiClient.GetDemographicsAsync(cancellationToken);

        var model = new StatisticsViewModel
        {
            Learning = learning,
            Demographics = demographics,
        };

        return View(model);
    }

    // AJAX cho dropdown granularity (daily/weekly/monthly).
    [HttpGet("/statistics/activity-trend")]
    public async Task<IActionResult> ActivityTrend(string granularity = "daily", CancellationToken cancellationToken = default)
    {
        var trend = await _apiClient.GetActivityTrendAsync(granularity, cancellationToken);
        if (trend is null)
        {
            return Json(new { success = false });
        }

        return Json(new
        {
            success = true,
            granularity = trend.Granularity,
            labels = trend.Points.Select(p => p.Period).ToArray(),
            sessions = trend.Points.Select(p => p.SessionsCount).ToArray(),
            accuracy = trend.Points.Select(p => p.Accuracy).ToArray(),
        });
    }
}
