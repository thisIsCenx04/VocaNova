using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Progress.BLL.Abstractions;
using VocaNova.API.Features.Progress.BLL.Models;
using VocaNova.API.Features.Progress.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Progress.DAL.Repositories;

public sealed class ProgressAnalyticsRepository : IProgressAnalyticsRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public ProgressAnalyticsRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DateTime>> GetSessionTimesAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default) =>
        await _dbContext.TestSessions
            .Where(session => session.UserId == userId
                && session.StartedAt >= fromInclusive
                && session.StartedAt < toExclusive)
            .Select(session => session.StartedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProgressAnswerStatistics>> GetAnswerStatisticsAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default) =>
        await _dbContext.TestAnswers
            .Where(answer => answer.Session.UserId == userId
                && answer.Session.StartedAt >= fromInclusive
                && answer.Session.StartedAt < toExclusive
                && answer.IsCorrect.HasValue)
            .Select(ProgressPersistenceMappings.ToAnswerStatistics)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<MasteryLevelCount>> GetMasteryLevelCountsAsync(
        uint userId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId)
            .GroupBy(progress => progress.MasteryLevel)
            .Select(group => new MasteryLevelCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<WeakestWordStatistics>> GetWeakestWordStatisticsAsync(
        uint userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.IsInWrongList)
            .OrderByDescending(progress => progress.WrongCount)
            .ThenByDescending(progress => progress.LastWrongAt)
            .ThenBy(progress => progress.Word.Word1)
            .Take(limit)
            .Select(ProgressPersistenceMappings.ToWeakestWordStatistics)
            .ToListAsync(cancellationToken);

    public async Task<WordProgressStatistics?> GetWordProgressStatisticsAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.WordId == wordId)
            .Select(ProgressPersistenceMappings.ToWordProgressStatistics)
            .SingleOrDefaultAsync(cancellationToken);
}
