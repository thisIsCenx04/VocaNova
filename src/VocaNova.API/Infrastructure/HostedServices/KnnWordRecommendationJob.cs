using Microsoft.Extensions.Options;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Services;

namespace VocaNova.API.Infrastructure.HostedServices;

public sealed class KnnWordRecommendationJob : BackgroundService
{
    private readonly IKnnRebuildService _rebuildService;
    private readonly KnnOptions _options;
    private readonly ILogger<KnnWordRecommendationJob> _logger;

    public KnnWordRecommendationJob(
        IKnnRebuildService rebuildService,
        IOptions<KnnOptions> options,
        ILogger<KnnWordRecommendationJob> logger)
    {
        _rebuildService = rebuildService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, _options.Learning.RebuildIntervalHours);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _rebuildService.RebuildAllAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "KNN word recommendation scheduled rebuild failed.");
            }
        }
    }
}
