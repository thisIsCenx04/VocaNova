using Microsoft.Extensions.Options;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Knn.Services;

public sealed class KnnRebuildService : IKnnRebuildService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKnnRebuildStateCache? _stateCache;
    private readonly ILogger<KnnRebuildService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly KnnOptions _options;

    public KnnRebuildService(
        IServiceScopeFactory scopeFactory,
        IOptions<KnnOptions> options,
        ILogger<KnnRebuildService> logger,
        IKnnRebuildStateCache? stateCache = null)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _stateCache = stateCache;
    }

    public bool IsRunning { get; private set; }

    public async Task<KnnRebuildStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var lastRebuildAt = _stateCache is null
            ? null
            : await _stateCache.GetLastRebuildAtAsync(cancellationToken);

        return new KnnRebuildStatusDto(lastRebuildAt, IsRunning);
    }

    public void TriggerRebuild()
    {
        _ = Task.Run(() => RebuildAllAsync(CancellationToken.None));
    }

    public async Task RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogInformation("KNN word recommendation rebuild is already running.");
            return;
        }

        IsRunning = true;
        var startedAt = DateTime.UtcNow;
        var processed = 0;
        var failed = 0;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IKnnLearningRepository>();
            var learningService = scope.ServiceProvider.GetRequiredService<IKnnLearningService>();
            var userIds = await repository.GetEligibleUserIdsAsync(
                _options.Learning.MinSessions,
                excludingUserId: 0,
                cancellationToken);

            foreach (var userId in userIds)
            {
                try
                {
                    var result = await learningService.GenerateWordRecommendationsAsync(userId, cancellationToken);
                    if (result.IsSuccess)
                    {
                        processed++;
                    }
                    else
                    {
                        failed++;
                        _logger.LogWarning(
                            "KNN word recommendation rebuild skipped user {UserId}: {Reason}",
                            userId,
                            result.Error);
                    }
                }
                catch (Exception exception)
                {
                    failed++;
                    _logger.LogError(exception, "KNN word recommendation rebuild failed for user {UserId}.", userId);
                }
            }

            var completedAt = DateTime.UtcNow;
            if (_stateCache is not null)
            {
                await _stateCache.SetLastRebuildAtAsync(completedAt, cancellationToken);
            }

            _logger.LogInformation(
                "KNN word recommendation rebuild finished. Processed={Processed}, Failed={Failed}, DurationMs={DurationMs}.",
                processed,
                failed,
                (completedAt - startedAt).TotalMilliseconds);
        }
        finally
        {
            IsRunning = false;
            _semaphore.Release();
        }
    }
}
