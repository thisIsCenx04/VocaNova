using VocaNova.API.Features.Progress.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IProgressSummaryCache
{
    Task<ProgressSummaryDto?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        ProgressSummaryDto summary,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
