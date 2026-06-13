using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Features.Progress.Repositories;

namespace VocaNova.API.Features.Progress.Services;

public sealed class ProgressAnalyticsService : IProgressAnalyticsService
{
    private const string Daily = "daily";
    private const string Weekly = "weekly";
    private const string Monthly = "monthly";
    private const int DefaultWeakestWordsLimit = 20;

    private static readonly IReadOnlySet<string> Granularities = new HashSet<string>
    {
        Daily,
        Weekly,
        Monthly,
    };

    private readonly IProgressAnalyticsRepository _progressAnalyticsRepository;

    public ProgressAnalyticsService(IProgressAnalyticsRepository progressAnalyticsRepository)
    {
        _progressAnalyticsRepository = progressAnalyticsRepository;
    }

    public async Task<Result<ProgressChartDto>> GetChartAsync(
        uint userId,
        string? granularity,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<ProgressChartDto>.Unauthorized("Unauthorized.");
        }

        var normalizedGranularity = string.IsNullOrWhiteSpace(granularity)
            ? Daily
            : granularity.Trim().ToLowerInvariant();
        if (!Granularities.Contains(normalizedGranularity))
        {
            return Result<ProgressChartDto>.Fail("Granularity must be daily, weekly, or monthly.");
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = BuildPeriods(normalizedGranularity, todayUtc);
        var fromInclusive = periods.First().Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = periods.Last().End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sessionTimes = await _progressAnalyticsRepository.GetSessionTimesAsync(
            userId,
            fromInclusive,
            toExclusive,
            cancellationToken);
        var answers = await _progressAnalyticsRepository.GetAnswerStatsRowsAsync(
            userId,
            fromInclusive,
            toExclusive,
            cancellationToken);

        var points = periods
            .Select(period => CreateChartPoint(period, sessionTimes, answers))
            .ToArray();

        return Result<ProgressChartDto>.Ok(new ProgressChartDto(normalizedGranularity, points));
    }

    public async Task<Result<IReadOnlyCollection<MasteryBreakdownDto>>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<MasteryBreakdownDto>>.Unauthorized("Unauthorized.");
        }

        var breakdown = await _progressAnalyticsRepository.GetMasteryBreakdownAsync(
            userId,
            cancellationToken);

        return Result<IReadOnlyCollection<MasteryBreakdownDto>>.Ok(breakdown);
    }

    public async Task<Result<IReadOnlyCollection<ProgressWeakestWordDto>>> GetWeakestWordsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<ProgressWeakestWordDto>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = limit ?? DefaultWeakestWordsLimit;
        if (normalizedLimit <= 0 || normalizedLimit > AppSettings.MaxPageLimit)
        {
            return Result<IReadOnlyCollection<ProgressWeakestWordDto>>.Fail(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var words = await _progressAnalyticsRepository.GetWeakestWordsAsync(
            userId,
            normalizedLimit,
            cancellationToken);

        return Result<IReadOnlyCollection<ProgressWeakestWordDto>>.Ok(words);
    }

    public async Task<Result<WordProgressDetailDto>> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<WordProgressDetailDto>.Unauthorized("Unauthorized.");
        }

        if (wordId == 0)
        {
            return Result<WordProgressDetailDto>.NotFound("Word progress not found.");
        }

        var progress = await _progressAnalyticsRepository.GetWordProgressAsync(
            userId,
            wordId,
            cancellationToken);
        if (progress is null)
        {
            return Result<WordProgressDetailDto>.NotFound("Word progress not found.");
        }

        return Result<WordProgressDetailDto>.Ok(progress);
    }

    private static IReadOnlyCollection<ChartPeriod> BuildPeriods(
        string granularity,
        DateOnly todayUtc)
    {
        return granularity switch
        {
            Daily => Enumerable.Range(0, 30)
                .Select(offset => todayUtc.AddDays(-29 + offset))
                .Select(date => new ChartPeriod(date, date, date.ToString("yyyy-MM-dd")))
                .ToArray(),
            Weekly => Enumerable.Range(0, 12)
                .Select(offset => StartOfWeek(todayUtc).AddDays((-11 + offset) * 7))
                .Select(start => new ChartPeriod(start, start.AddDays(6), start.ToString("yyyy-MM-dd")))
                .ToArray(),
            Monthly => Enumerable.Range(0, 6)
                .Select(offset => new DateOnly(todayUtc.Year, todayUtc.Month, 1).AddMonths(-5 + offset))
                .Select(start => new ChartPeriod(
                    start,
                    start.AddMonths(1).AddDays(-1),
                    start.ToString("yyyy-MM")))
                .ToArray(),
            _ => Array.Empty<ChartPeriod>(),
        };
    }

    private static ProgressChartPointDto CreateChartPoint(
        ChartPeriod period,
        IReadOnlyCollection<DateTime> sessionTimes,
        IReadOnlyCollection<ProgressAnswerStatsRow> answers)
    {
        var sessionsCount = sessionTimes.Count(startedAt => IsInPeriod(startedAt, period));
        var periodAnswers = answers
            .Where(answer => IsInPeriod(answer.SessionStartedAt, period))
            .ToArray();
        var correctCount = periodAnswers.Count(answer => answer.IsCorrect);
        var totalAnswers = periodAnswers.Length;
        var accuracy = totalAnswers == 0 ? 0 : (float)correctCount / totalAnswers * 100;

        return new ProgressChartPointDto(
            period.Start,
            period.End,
            period.Label,
            sessionsCount,
            correctCount,
            totalAnswers,
            accuracy);
    }

    private static bool IsInPeriod(DateTime value, ChartPeriod period)
    {
        var date = DateOnly.FromDateTime(value);
        return date >= period.Start && date <= period.End;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private sealed record ChartPeriod(DateOnly Start, DateOnly End, string Label);
}
