using VocaNova.API.Common.Results;
using VocaNova.API.Features.Progress.DTOs;

namespace VocaNova.API.Features.Progress.Services;

public interface IProgressAnalyticsService
{
    Task<Result<ProgressChartDto>> GetChartAsync(
        uint userId,
        string? granularity,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<MasteryBreakdownDto>>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<ProgressWeakestWordDto>>> GetWeakestWordsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<Result<WordProgressDetailDto>> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
