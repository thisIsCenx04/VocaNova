using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.Dashboard.Models.Api.Stats;
using VocaNova.Dashboard.Models.Statistics;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Controllers;

[Authorize]
public sealed class StatisticsController : Controller
{
    private readonly IVocaNovaApiClient _api;

    public StatisticsController(IVocaNovaApiClient api)
    {
        _api = api;
    }

    [HttpGet("/statistics")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var learning = await _api.GetAsync<AdminLearningStatsDto>("api/admin/stats/learning", cancellationToken);
        var demographics = await _api.GetAsync<AdminDemographicsDto>("api/admin/stats/demographics", cancellationToken);

        return View(new StatisticsViewModel
        {
            Learning = learning.IsSuccess ? learning.Data : null,
            LearningLoaded = learning.IsSuccess && learning.Data is not null,
            Demographics = demographics.IsSuccess ? demographics.Data : null,
            DemographicsLoaded = demographics.IsSuccess && demographics.Data is not null,
            // G7: granularity chưa có API → dropdown disabled, tạm fix daily.
            GranularityAvailable = false,
        });
    }
}
