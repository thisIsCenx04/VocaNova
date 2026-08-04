using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Progress.Repositories;

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
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestSessions
            .Where(session => session.UserId == userId
                && session.StartedAt >= fromInclusive
                && session.StartedAt < toExclusive)
            .Select(session => session.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProgressAnswerStatsRow>> GetAnswerStatsRowsAsync(
        uint userId,
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestAnswers
            .Where(answer => answer.Session.UserId == userId
                && answer.Session.StartedAt >= fromInclusive
                && answer.Session.StartedAt < toExclusive
                && answer.IsCorrect.HasValue)
            .Select(answer => new ProgressAnswerStatsRow(
                answer.Session.StartedAt,
                answer.IsCorrect == true))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MasteryBreakdownDto>> GetMasteryBreakdownAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId)
            .GroupBy(progress => progress.MasteryLevel)
            .Select(group => new { MasteryLevel = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var countByLevel = counts.ToDictionary(item => item.MasteryLevel, item => item.Count);
        return Enumerable.Range(0, 6)
            .Select(level => new MasteryBreakdownDto(
                level,
                countByLevel.GetValueOrDefault(level)))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<ProgressWeakestWordDto>> GetWeakestWordsAsync(
        uint userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.IsInWrongList)
            .OrderByDescending(progress => progress.WrongCount)
            .ThenByDescending(progress => progress.LastWrongAt)
            .ThenBy(progress => progress.Word.Word1)
            .Take(limit)
            .Select(progress => new
            {
                progress.WordId,
                Word = progress.Word.Word1,
                PrimaryMeaning = progress.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                progress.TestCount,
                progress.CorrectCount,
                progress.WrongCount,
                progress.MasteryLevel,
                progress.LastWrongAt,
                progress.NextReviewAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ProgressWeakestWordDto(
                row.WordId,
                row.Word,
                row.PrimaryMeaning,
                row.TestCount,
                row.CorrectCount,
                row.WrongCount,
                CalculateAccuracy(row.CorrectCount, row.TestCount),
                row.MasteryLevel,
                row.LastWrongAt,
                row.NextReviewAt))
            .ToArray();
    }

    public async Task<WordProgressDetailDto?> GetWordProgressAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.WordId == wordId)
            .Select(progress => new
            {
                progress.WordId,
                Word = progress.Word.Word1,
                PrimaryMeaning = progress.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                progress.TestCount,
                progress.CorrectCount,
                progress.WrongCount,
                progress.ConsecutiveCorrect,
                progress.IsInWrongList,
                progress.MasteryLevel,
                progress.SrsInterval,
                progress.EaseFactor,
                progress.LastTestedAt,
                progress.LastWrongAt,
                progress.NextReviewAt,
                progress.UpdatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new WordProgressDetailDto(
                row.WordId,
                row.Word,
                row.PrimaryMeaning,
                row.TestCount,
                row.CorrectCount,
                row.WrongCount,
                CalculateAccuracy(row.CorrectCount, row.TestCount),
                row.ConsecutiveCorrect,
                row.IsInWrongList,
                row.MasteryLevel,
                row.SrsInterval,
                row.EaseFactor,
                row.LastTestedAt,
                row.LastWrongAt,
                row.NextReviewAt,
                row.UpdatedAt);
    }

    private static float CalculateAccuracy(int correctCount, int testCount)
    {
        return testCount == 0 ? 0 : (float)correctCount / testCount * 100;
    }
}
