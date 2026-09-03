using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Abstractions;

public interface IProgressAnalyticsRepository
{
    Task<IReadOnlyCollection<DateTime>> GetSessionTimesAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProgressAnswerStatistics>> GetAnswerStatisticsAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MasteryLevelCount>> GetMasteryLevelCountsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WeakestWordStatistics>> GetWeakestWordStatisticsAsync(
        uint userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<WordProgressStatistics?> GetWordProgressStatisticsAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
