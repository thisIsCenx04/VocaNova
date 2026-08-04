using VocaNova.API.Common.Results;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Features.Progress.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Progress.Services;

public sealed class ProgressSummaryService : IProgressSummaryService
{
    private readonly IProgressSummaryRepository _progressSummaryRepository;
    private readonly IProgressSummaryCache? _progressSummaryCache;

    public ProgressSummaryService(
        IProgressSummaryRepository progressSummaryRepository,
        IProgressSummaryCache? progressSummaryCache = null)
    {
        _progressSummaryRepository = progressSummaryRepository;
        _progressSummaryCache = progressSummaryCache;
    }

    public async Task<Result<ProgressSummaryDto>> GetSummaryAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<ProgressSummaryDto>.Unauthorized("Unauthorized.");
        }

        if (_progressSummaryCache is not null)
        {
            var cached = await _progressSummaryCache.GetAsync(userId, cancellationToken);
            if (cached is not null)
            {
                return Result<ProgressSummaryDto>.Ok(cached);
            }
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var stats = await _progressSummaryRepository.GetSummaryStatsAsync(
            userId,
            todayUtc,
            cancellationToken);

        var summary = ToSummary(stats, todayUtc);

        if (_progressSummaryCache is not null)
        {
            await _progressSummaryCache.SetAsync(userId, summary, cancellationToken);
        }

        return Result<ProgressSummaryDto>.Ok(summary);
    }

    private static ProgressSummaryDto ToSummary(
        ProgressSummaryStats stats,
        DateOnly todayUtc)
    {
        var distinctDates = stats.SessionDates
            .Distinct()
            .OrderBy(date => date)
            .ToArray();

        var currentStreak = CalculateCurrentStreak(distinctDates, todayUtc);
        var longestStreak = CalculateLongestStreak(distinctDates);
        var accuracy = stats.TotalAnswers7Days == 0
            ? 0
            : (float)stats.Correct7Days / stats.TotalAnswers7Days * 100;

        return new ProgressSummaryDto(
            currentStreak,
            longestStreak,
            accuracy,
            stats.Correct7Days,
            stats.TotalAnswers7Days,
            stats.TotalWordsInProgress,
            stats.MasteredWords,
            stats.SessionsThisMonth);
    }

    private static int CalculateCurrentStreak(
        IReadOnlyCollection<DateOnly> sessionDates,
        DateOnly todayUtc)
    {
        if (sessionDates.Count == 0)
        {
            return 0;
        }

        var dateSet = sessionDates.ToHashSet();
        var cursor = dateSet.Contains(todayUtc) ? todayUtc : todayUtc.AddDays(-1);
        var streak = 0;

        while (dateSet.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyCollection<DateOnly> sessionDates)
    {
        var longest = 0;
        var current = 0;
        DateOnly? previous = null;

        foreach (var date in sessionDates.OrderBy(date => date))
        {
            current = previous.HasValue && date.DayNumber == previous.Value.DayNumber + 1
                ? current + 1
                : 1;

            longest = Math.Max(longest, current);
            previous = date;
        }

        return longest;
    }
}
