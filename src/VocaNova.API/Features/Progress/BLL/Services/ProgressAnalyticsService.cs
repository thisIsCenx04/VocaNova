using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Services;

public sealed class ProgressAnalyticsService : IProgressAnalyticsService
{
    public const int MaximumWeakestWordsLimit = 100;

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

    public async Task<ProgressResult<ProgressChart>> GetChartAsync(
        uint userId,
        ProgressChartQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ProgressResult<ProgressChart>.Unauthorized("Unauthorized.");
        }

        var granularity = string.IsNullOrWhiteSpace(query.Granularity)
            ? Daily
            : query.Granularity.Trim().ToLowerInvariant();
        if (!Granularities.Contains(granularity))
        {
            return ProgressResult<ProgressChart>.ValidationFailure(
                "Granularity must be daily, weekly, or monthly.");
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = BuildPeriods(granularity, todayUtc);
        var fromInclusive = periods.First().Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = periods.Last().End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var sessionTimes = await _progressAnalyticsRepository.GetSessionTimesAsync(
            userId,
            fromInclusive,
            toExclusive,
            cancellationToken);
        var answers = await _progressAnalyticsRepository.GetAnswerStatisticsAsync(
            userId,
            fromInclusive,
            toExclusive,
            cancellationToken);
        var points = periods
            .Select(period => CreateChartPoint(period, sessionTimes, answers))
            .ToArray();

        return ProgressResult<ProgressChart>.Success(new ProgressChart(granularity, points));
    }

    public async Task<ProgressResult<IReadOnlyCollection<MasteryBreakdown>>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ProgressResult<IReadOnlyCollection<MasteryBreakdown>>.Unauthorized("Unauthorized.");
        }

        var counts = await _progressAnalyticsRepository.GetMasteryLevelCountsAsync(
            userId,
            cancellationToken);
        var countByLevel = counts.ToDictionary(item => item.MasteryLevel, item => item.WordCount);
        var breakdown = Enumerable.Range(0, 6)
            .Select(level => new MasteryBreakdown(level, countByLevel.GetValueOrDefault(level)))
            .ToArray();

        return ProgressResult<IReadOnlyCollection<MasteryBreakdown>>.Success(breakdown);
    }

    public async Task<ProgressResult<IReadOnlyCollection<WeakestWord>>> GetWeakestWordsAsync(
        uint userId,
        WeakestWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ProgressResult<IReadOnlyCollection<WeakestWord>>.Unauthorized("Unauthorized.");
        }

        var limit = query.Limit ?? DefaultWeakestWordsLimit;
        if (limit <= 0 || limit > MaximumWeakestWordsLimit)
        {
            return ProgressResult<IReadOnlyCollection<WeakestWord>>.ValidationFailure(
                $"Limit must be between 1 and {MaximumWeakestWordsLimit}.");
        }

        var statistics = await _progressAnalyticsRepository.GetWeakestWordStatisticsAsync(
            userId,
            limit,
            cancellationToken);
        var words = statistics.Select(ToWeakestWord).ToArray();

        return ProgressResult<IReadOnlyCollection<WeakestWord>>.Success(words);
    }

    public async Task<ProgressResult<WordProgress>> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ProgressResult<WordProgress>.Unauthorized("Unauthorized.");
        }

        if (wordId == 0)
        {
            return ProgressResult<WordProgress>.NotFound("Word progress not found.");
        }

        var statistics = await _progressAnalyticsRepository.GetWordProgressStatisticsAsync(
            userId,
            wordId,
            cancellationToken);
        if (statistics is null)
        {
            return ProgressResult<WordProgress>.NotFound("Word progress not found.");
        }

        return ProgressResult<WordProgress>.Success(ToWordProgress(statistics));
    }

    private static IReadOnlyCollection<ChartPeriod> BuildPeriods(
        string granularity,
        DateOnly todayUtc) =>
        granularity switch
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

    private static ProgressChartPoint CreateChartPoint(
        ChartPeriod period,
        IReadOnlyCollection<DateTime> sessionTimes,
        IReadOnlyCollection<ProgressAnswerStatistics> answers)
    {
        var sessionsCount = sessionTimes.Count(startedAt => IsInPeriod(startedAt, period));
        var periodAnswers = answers
            .Where(answer => IsInPeriod(answer.SessionStartedAt, period))
            .ToArray();
        var correctCount = periodAnswers.Count(answer => answer.IsCorrect);
        var totalAnswers = periodAnswers.Length;

        return new ProgressChartPoint(
            period.Start,
            period.End,
            period.Label,
            sessionsCount,
            correctCount,
            totalAnswers,
            CalculateAccuracy(correctCount, totalAnswers));
    }

    private static WeakestWord ToWeakestWord(WeakestWordStatistics statistics) =>
        new(
            statistics.WordId,
            statistics.Word,
            statistics.PrimaryMeaning,
            statistics.TestCount,
            statistics.CorrectCount,
            statistics.WrongCount,
            CalculateAccuracy(statistics.CorrectCount, statistics.TestCount),
            statistics.MasteryLevel,
            statistics.LastWrongAt,
            statistics.NextReviewAt);

    private static WordProgress ToWordProgress(WordProgressStatistics statistics) =>
        new(
            statistics.WordId,
            statistics.Word,
            statistics.PrimaryMeaning,
            statistics.TestCount,
            statistics.CorrectCount,
            statistics.WrongCount,
            CalculateAccuracy(statistics.CorrectCount, statistics.TestCount),
            statistics.ConsecutiveCorrect,
            statistics.IsInWrongList,
            statistics.MasteryLevel,
            statistics.SrsInterval,
            statistics.EaseFactor,
            statistics.LastTestedAt,
            statistics.LastWrongAt,
            statistics.NextReviewAt,
            statistics.UpdatedAt);

    private static float CalculateAccuracy(int correctCount, int totalAnswers) =>
        totalAnswers == 0 ? 0 : (float)correctCount / totalAnswers * 100;

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
