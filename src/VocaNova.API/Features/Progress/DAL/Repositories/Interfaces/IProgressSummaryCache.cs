using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Abstractions;

public interface IProgressSummaryCache
{
    Task<ProgressSummary?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        ProgressSummary summary,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
