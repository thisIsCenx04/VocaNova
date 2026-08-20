using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Services;

public interface IProgressAnalyticsService
{
    Task<ProgressResult<ProgressChart>> GetChartAsync(
        uint userId,
        ProgressChartQuery query,
        CancellationToken cancellationToken = default);

    Task<ProgressResult<IReadOnlyCollection<MasteryBreakdown>>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<ProgressResult<IReadOnlyCollection<WeakestWord>>> GetWeakestWordsAsync(
        uint userId,
        WeakestWordsQuery query,
        CancellationToken cancellationToken = default);

    Task<ProgressResult<WordProgress>> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
