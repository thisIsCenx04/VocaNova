using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Abstractions;

public interface IProgressSummaryRepository
{
    Task<ProgressSummaryStatistics> GetSummaryStatisticsAsync(
        uint userId,
        ProgressSummaryQuery query,
        CancellationToken cancellationToken = default);
}
