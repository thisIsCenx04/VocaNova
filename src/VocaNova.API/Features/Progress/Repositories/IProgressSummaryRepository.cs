using VocaNova.API.Features.Progress.DTOs;

namespace VocaNova.API.Features.Progress.Repositories;

public interface IProgressSummaryRepository
{
    Task<ProgressSummaryStats> GetSummaryStatsAsync(
        uint userId,
        DateOnly todayUtc,
        CancellationToken cancellationToken = default);
}
