using Microsoft.Extensions.Caching.Memory;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Models;

namespace VocaNova.API.Features.Admin.BLL.Services;

public sealed class AdminStatsService : IAdminStatsService
{
    private const string DashboardCacheKey = "admin:stats:dashboard";
    private const int DashboardCacheMinutes = 5;
    private const int TopWrongWordLimit = 20;
    private const int TrendDays = 30;
    private const int MaxSessionsTrendDays = 90;
    private const int MasteryLevelCount = 6; // mastery_level 0..5

    private readonly IAdminStatsRepository _repository;
    private readonly IMemoryCache _cache;

    public AdminStatsService(
        IAdminStatsRepository repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<AdminDashboardStatsModel>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _cache.GetOrCreateAsync(DashboardCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(DashboardCacheMinutes);

            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            var sevenDayStartUtc = todayUtc.AddDays(-6);
            return await _repository.GetDashboardStatsAsync(
                todayUtc,
                tomorrowUtc,
                sevenDayStartUtc,
                cancellationToken);
        });

        return Result<AdminDashboardStatsModel>.Ok(stats!);
    }

    public async Task<Result<AdminDemographicsModel>> GetDemographicsAsync(
        CancellationToken cancellationToken = default)
    {
        var demographics = await _repository.GetDemographicsAsync(cancellationToken);
        return Result<AdminDemographicsModel>.Ok(demographics);
    }

    public async Task<Result<AdminLearningStatsModel>> GetLearningStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var trendStartUtc = todayUtc.AddDays(-(TrendDays - 1));
        var topWrongWords = await _repository.GetTopWrongWordsAsync(TopWrongWordLimit, cancellationToken);
        var trendRows = await _repository.GetSessionAccuracyRowsAsync(
            trendStartUtc,
            tomorrowUtc,
            cancellationToken);
        var trendByDate = trendRows.ToDictionary(row => row.Date);
        var trend = Enumerable
            .Range(0, TrendDays)
            .Select(offset =>
            {
                var date = DateOnly.FromDateTime(trendStartUtc.AddDays(offset));
                if (!trendByDate.TryGetValue(date, out var row))
                {
                    return new AdminAccuracyTrendPointModel(date, 0, 0, 0, 0);
                }

                var total = row.CorrectCount + row.WrongCount;
                var accuracy = total == 0
                    ? 0
                    : Math.Round(row.CorrectCount * 100d / total, 2);
                return new AdminAccuracyTrendPointModel(
                    date,
                    row.CorrectCount,
                    row.WrongCount,
                    total,
                    accuracy);
            })
            .ToArray();

        return Result<AdminLearningStatsModel>.Ok(new AdminLearningStatsModel(topWrongWords, trend));
    }

    public async Task<Result<AdminSessionsTrendModel>> GetSessionsTrendAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > MaxSessionsTrendDays)
        {
            return Result<AdminSessionsTrendModel>.Fail($"Days must be between 1 and {MaxSessionsTrendDays}.");
        }

        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var startUtc = todayUtc.AddDays(-(days - 1));
        var rows = await _repository.GetSessionCountsByDayAsync(startUtc, tomorrowUtc, cancellationToken);
        var countsByDate = rows.ToDictionary(row => row.Date, row => row.SessionCount);

        // Bù đủ N ngày (kể cả ngày không có phiên = 0) để chart liền mạch.
        var points = Enumerable
            .Range(0, days)
            .Select(offset =>
            {
                var date = DateOnly.FromDateTime(startUtc.AddDays(offset));
                return new AdminSessionTrendPointModel(
                    date,
                    countsByDate.TryGetValue(date, out var count) ? count : 0);
            })
            .ToArray();

        return Result<AdminSessionsTrendModel>.Ok(new AdminSessionsTrendModel(days, points));
    }

    public async Task<Result<AdminMasteryDistributionModel>> GetMasteryDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _repository.GetMasteryDistributionAsync(cancellationToken);
        var countsByLevel = rows.ToDictionary(row => row.Level, row => row.Count);

        // Luôn trả đủ level 0..5 (level rỗng = 0) để pie chart ổn định.
        var levels = Enumerable
            .Range(0, MasteryLevelCount)
            .Select(level => new AdminMasteryLevelModel(
                level,
                countsByLevel.TryGetValue(level, out var count) ? count : 0))
            .ToArray();

        var total = levels.Sum(level => level.WordCount);
        return Result<AdminMasteryDistributionModel>.Ok(new AdminMasteryDistributionModel(total, levels));
    }

    public async Task<Result<AdminActivityTrendModel>> GetActivityTrendAsync(
        string granularity,
        CancellationToken cancellationToken = default)
    {
        var g = (granularity ?? string.Empty).Trim().ToLowerInvariant();
        if (g != "daily" && g != "weekly" && g != "monthly")
        {
            return Result<AdminActivityTrendModel>.Fail("Granularity must be daily, weekly, or monthly.");
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Mỗi bucket = (label, ngày đầu, ngày cuối inclusive).
        var buckets = new List<(string Period, DateOnly Start, DateOnly End)>();
        DateTime windowStart;

        if (g == "daily")
        {
            windowStart = today.AddDays(-29); // 30 ngày
            for (var i = 0; i < 30; i++)
            {
                var d = windowStart.AddDays(i);
                var only = DateOnly.FromDateTime(d);
                buckets.Add((d.ToString("yyyy-MM-dd"), only, only));
            }
        }
        else if (g == "weekly")
        {
            var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // thứ Hai
            windowStart = startOfWeek.AddDays(-7 * 11); // 12 tuần
            for (var i = 0; i < 12; i++)
            {
                var ws = windowStart.AddDays(7 * i);
                buckets.Add((ws.ToString("yyyy-MM-dd"), DateOnly.FromDateTime(ws), DateOnly.FromDateTime(ws.AddDays(6))));
            }
        }
        else
        {
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);
            windowStart = firstOfMonth.AddMonths(-5); // 6 tháng
            for (var i = 0; i < 6; i++)
            {
                var ms = windowStart.AddMonths(i);
                buckets.Add((ms.ToString("yyyy-MM"), DateOnly.FromDateTime(ms), DateOnly.FromDateTime(ms.AddMonths(1).AddDays(-1))));
            }
        }

        var accuracyRows = await _repository.GetSessionAccuracyRowsAsync(windowStart, tomorrow, cancellationToken);
        var countRows = await _repository.GetSessionCountsByDayAsync(windowStart, tomorrow, cancellationToken);
        var accuracyByDate = accuracyRows.ToDictionary(row => row.Date);
        var countByDate = countRows.ToDictionary(row => row.Date, row => row.SessionCount);

        var points = buckets
            .Select(bucket =>
            {
                var sessions = 0;
                var correct = 0;
                var total = 0;
                for (var d = bucket.Start; d <= bucket.End; d = d.AddDays(1))
                {
                    if (countByDate.TryGetValue(d, out var c))
                    {
                        sessions += c;
                    }

                    if (accuracyByDate.TryGetValue(d, out var row))
                    {
                        correct += row.CorrectCount;
                        total += row.CorrectCount + row.WrongCount;
                    }
                }

                var accuracy = total == 0 ? 0 : Math.Round(correct * 100d / total, 2);
                return new AdminActivityTrendPointModel(bucket.Period, sessions, correct, total, accuracy);
            })
            .ToArray();

        return Result<AdminActivityTrendModel>.Ok(new AdminActivityTrendModel(g, points));
    }

    public async Task<Result<PagedResult<AdminAuditLogModel>>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<AdminAuditLogModel>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminAuditLogModel>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var normalized = query with
        {
            Entity = string.IsNullOrWhiteSpace(query.Entity) ? null : query.Entity.Trim(),
        };
        var result = await _repository.GetAuditLogsAsync(normalized, cancellationToken);
        return Result<PagedResult<AdminAuditLogModel>>.Ok(result);
    }
}
