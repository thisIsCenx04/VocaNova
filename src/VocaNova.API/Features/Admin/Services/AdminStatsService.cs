using Microsoft.Extensions.Caching.Memory;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;

namespace VocaNova.API.Features.Admin.Services;

public sealed class AdminStatsService : IAdminStatsService
{
    private const string DashboardCacheKey = "admin:stats:dashboard";
    private const int DashboardCacheMinutes = 5;
    private const int TopWrongWordLimit = 20;
    private const int TrendDays = 30;

    private readonly IAdminStatsRepository _repository;
    private readonly IMemoryCache _cache;

    public AdminStatsService(
        IAdminStatsRepository repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Result<AdminDashboardStatsDto>> GetDashboardAsync(
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

        return Result<AdminDashboardStatsDto>.Ok(stats!);
    }

    public async Task<Result<AdminDemographicsDto>> GetDemographicsAsync(
        CancellationToken cancellationToken = default)
    {
        var demographics = await _repository.GetDemographicsAsync(cancellationToken);
        return Result<AdminDemographicsDto>.Ok(demographics);
    }

    public async Task<Result<AdminLearningStatsDto>> GetLearningStatsAsync(
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
                    return new AdminAccuracyTrendPointDto(date, 0, 0, 0, 0);
                }

                var total = row.CorrectCount + row.WrongCount;
                var accuracy = total == 0
                    ? 0
                    : Math.Round(row.CorrectCount * 100d / total, 2);
                return new AdminAccuracyTrendPointDto(
                    date,
                    row.CorrectCount,
                    row.WrongCount,
                    total,
                    accuracy);
            })
            .ToArray();

        return Result<AdminLearningStatsDto>.Ok(new AdminLearningStatsDto(topWrongWords, trend));
    }

    public async Task<Result<PagedResult<AdminAuditLogDto>>> GetAuditLogsAsync(
        AdminAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<AdminAuditLogDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminAuditLogDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var normalized = query with
        {
            Entity = string.IsNullOrWhiteSpace(query.Entity) ? null : query.Entity.Trim(),
        };
        var result = await _repository.GetAuditLogsAsync(normalized, cancellationToken);
        return Result<PagedResult<AdminAuditLogDto>>.Ok(result);
    }
}
