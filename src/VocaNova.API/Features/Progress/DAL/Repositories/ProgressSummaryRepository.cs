using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Progress.DAL.Repositories;

public sealed class ProgressSummaryRepository : IProgressSummaryRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public ProgressSummaryRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProgressSummaryStatistics> GetSummaryStatisticsAsync(
        uint userId,
        ProgressSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var sessionDates = await _dbContext.TestSessions
            .Where(session => session.UserId == userId
                && session.StartedAt < query.TomorrowExclusive)
            .Select(session => session.StartedAt.Date)
            .Distinct()
            .ToListAsync(cancellationToken);
        var answerStatistics = await _dbContext.TestAnswers
            .Where(answer => answer.Session.UserId == userId
                && answer.Session.StartedAt >= query.SevenDayStartInclusive
                && answer.Session.StartedAt < query.TomorrowExclusive
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
        var masteredWords = await _dbContext.UserWordProgresses.CountAsync(
            progress => progress.UserId == userId
                && progress.MasteryLevel >= query.MasteredLevel,
            cancellationToken);
        var sessionsThisMonth = await _dbContext.TestSessions.CountAsync(
            session => session.UserId == userId
                && session.StartedAt >= query.MonthStartInclusive
                && session.StartedAt < query.TomorrowExclusive,
            cancellationToken);

        return new ProgressSummaryStatistics(
            sessionDates.Select(DateOnly.FromDateTime).ToArray(),
            answerStatistics?.Correct ?? 0,
            answerStatistics?.Total ?? 0,
            totalWordsInProgress,
            masteredWords,
            sessionsThisMonth);
    }
}
