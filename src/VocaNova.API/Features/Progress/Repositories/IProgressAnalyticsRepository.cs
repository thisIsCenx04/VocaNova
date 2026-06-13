using VocaNova.API.Features.Progress.DTOs;

namespace VocaNova.API.Features.Progress.Repositories;

public interface IProgressAnalyticsRepository
{
    Task<IReadOnlyCollection<DateTime>> GetSessionTimesAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProgressAnswerStatsRow>> GetAnswerStatsRowsAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MasteryBreakdownDto>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProgressWeakestWordDto>> GetWeakestWordsAsync(
        uint userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<WordProgressDetailDto?> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
