using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Progress.Repositories;

public sealed class ProgressSummaryRepository : IProgressSummaryRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public ProgressSummaryRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProgressSummaryStats> GetSummaryStatsAsync(
        uint userId,
        DateOnly todayUtc,
        CancellationToken cancellationToken = default)
    {
        var sevenDayStart = todayUtc.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var tomorrow = todayUtc.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var monthStart = new DateOnly(todayUtc.Year, todayUtc.Month, 1)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sessionDates = await _dbContext.TestSessions
            .Where(session => session.UserId == userId && session.StartedAt < tomorrow)
            .Select(session => session.StartedAt.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        var answerStats = await _dbContext.TestAnswers
            .Where(answer => answer.Session.UserId == userId
                && answer.Session.StartedAt >= sevenDayStart
                && answer.Session.StartedAt < tomorrow
                && answer.IsCorrect.HasValue)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Correct = group.Count(answer => answer.IsCorrect == true),
                Total = group.Count(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var totalWordsInProgress = await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId)
            .Select(progress => progress.WordId)
            .Distinct()
            .CountAsync(cancellationToken);

        var masteredWords = await _dbContext.UserWordProgresses
            .CountAsync(
                progress => progress.UserId == userId && progress.MasteryLevel >= 5,
                cancellationToken);

        var sessionsThisMonth = await _dbContext.TestSessions
            .CountAsync(
                session => session.UserId == userId
                    && session.StartedAt >= monthStart
                    && session.StartedAt < tomorrow,
                cancellationToken);

        return new ProgressSummaryStats(
            sessionDates.Select(DateOnly.FromDateTime).ToArray(),
            answerStats?.Correct ?? 0,
            answerStats?.Total ?? 0,
            totalWordsInProgress,
            masteredWords,
            sessionsThisMonth);
    }
}
