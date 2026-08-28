using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Features.Progress.BLL.Services.IServices;

namespace VocaNova.API.Features.Progress.BLL.Services;

public sealed class ProgressSummaryService : IProgressSummaryService
{
    private const int MasteredLevel = 5;

    private readonly IProgressSummaryRepository _progressSummaryRepository;
    private readonly IProgressSummaryCache? _progressSummaryCache;

    public ProgressSummaryService(
        IProgressSummaryRepository progressSummaryRepository,
        IProgressSummaryCache? progressSummaryCache = null)
    {
        _progressSummaryRepository = progressSummaryRepository;
        _progressSummaryCache = progressSummaryCache;
    }

    public async Task<ProgressResult<ProgressSummary>> GetSummaryAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ProgressResult<ProgressSummary>.Unauthorized("Unauthorized.");
        }

        if (_progressSummaryCache is not null)
        {
            var cached = await _progressSummaryCache.GetAsync(userId, cancellationToken);
            if (cached is not null)
            {
                return ProgressResult<ProgressSummary>.Success(cached);
            }
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = new ProgressSummaryQuery(
            todayUtc.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            todayUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            new DateOnly(todayUtc.Year, todayUtc.Month, 1)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            MasteredLevel);
        var statistics = await _progressSummaryRepository.GetSummaryStatisticsAsync(
            userId,
            query,
            cancellationToken);
        var summary = CreateSummary(statistics, todayUtc);

        if (_progressSummaryCache is not null)
        {
            await _progressSummaryCache.SetAsync(userId, summary, cancellationToken);
        }

        return ProgressResult<ProgressSummary>.Success(summary);
    }

    private static ProgressSummary CreateSummary(
        ProgressSummaryStatistics statistics,
        DateOnly todayUtc)
    {
        var distinctDates = statistics.SessionDates
            .Distinct()
            .OrderBy(date => date)
            .ToArray();
        var accuracy = statistics.TotalAnswers7Days == 0
            ? 0
            : (float)statistics.Correct7Days / statistics.TotalAnswers7Days * 100;

        return new ProgressSummary(
            CalculateCurrentStreak(distinctDates, todayUtc),
            CalculateLongestStreak(distinctDates),
            accuracy,
            statistics.Correct7Days,
            statistics.TotalAnswers7Days,
            statistics.TotalWordsInProgress,
            statistics.MasteredWords,
            statistics.SessionsThisMonth);
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
