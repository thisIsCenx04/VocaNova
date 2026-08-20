using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Knn.DAL.Repositories;

public sealed class KnnLearningRepository : IKnnLearningRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public KnnLearningRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> GetSessionCountAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TestSessions.CountAsync(
            session => session.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<uint>> GetActiveTopicIdsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(topic => topic.TopicId)
            .Select(topic => topic.TopicId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnnTopicAnswerStatistics>> GetTopicAnswerStatsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TestAnswers
            .AsNoTracking()
            .Where(answer => answer.Session.UserId == userId && answer.IsCorrect.HasValue)
            .SelectMany(answer => answer.Word.WordTopics.Select(wordTopic => new
            {
                wordTopic.TopicId,
                IsCorrect = answer.IsCorrect == true,
            }))
            .GroupBy(row => row.TopicId)
            .Select(group => new
            {
                TopicId = group.Key,
                CorrectCount = group.Count(row => row.IsCorrect),
                TotalCount = group.Count(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new KnnTopicAnswerStatistics(
                row.TopicId,
                row.CorrectCount,
                row.TotalCount))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<uint>> GetEligibleUserIdsAsync(
        int minSessions,
        uint excludingUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TestSessions
            .AsNoTracking()
            .Where(session => session.UserId != excludingUserId
                && session.User.Status == UserStatus.Active)
            .GroupBy(session => session.UserId)
            .Where(group => group.Count() >= minSessions)
            .Select(group => group.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnnMasteredWord>> GetMasteredWordsAsync(
        IReadOnlyCollection<uint> userIds,
        int minMasteryLevel,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<KnnMasteredWord>();
        }

        return await _dbContext.UserWordProgresses
            .AsNoTracking()
            .Where(progress => userIds.Contains(progress.UserId)
                && progress.MasteryLevel >= minMasteryLevel)
            .Select(progress => new KnnMasteredWord(progress.UserId, progress.WordId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnnNeighborWord>> GetNeighborStudiedWordsAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<KnnNeighborWord>();
        }

        var query = _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => userIds.Contains(listWord.UserId)
                && listWord.Status == UserStatus.Active);

        if (topicIds.Count > 0)
        {
            var topicIdValues = topicIds.ToArray();
            query = query.Where(listWord => listWord.Word.WordTopics
                .Any(wordTopic => topicIdValues.Contains(wordTopic.TopicId)));
        }

        return await query
            .Select(listWord => new KnnNeighborWord(listWord.UserId, listWord.WordId))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<uint>> GetActiveListWordIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.Status == UserStatus.Active)
            .Select(listWord => listWord.WordId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WordRecommendationItem>> GetWordRecommendationItemsAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetWordRowsAsync(scoresByWordId.Keys.ToArray(), cancellationToken);

        return rows
            .Select(row => new WordRecommendationItem(
                row.WordId,
                row.Word,
                row.PhoneticUk,
                row.PrimaryMeaning,
                row.ImageUrl,
                row.CefrLevel,
                scoresByWordId[row.WordId]))
            .OrderByDescending(word => word.Score)
            .ThenBy(word => word.Word)
            .ThenBy(word => word.WordId)
            .Take(limit)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<WordRecommendation>> GetWordRecommendationsAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetWordRowsAsync(scoresByWordId.Keys.ToArray(), cancellationToken);

        return rows
            .Select(row => new WordRecommendation(
                row.WordId,
                row.Word,
                row.PhoneticUk,
                row.PrimaryMeaning,
                row.ImageUrl,
                row.CefrLevel,
                scoresByWordId[row.WordId]))
            .OrderByDescending(word => word.Score)
            .ThenBy(word => word.Word)
            .ThenBy(word => word.WordId)
            .Take(limit)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<WordRow>> GetWordRowsAsync(
        IReadOnlyCollection<uint> wordIds,
        CancellationToken cancellationToken)
    {
        if (wordIds.Count == 0)
        {
            return Array.Empty<WordRow>();
        }

        return await _dbContext.Words
            .AsNoTracking()
            .Where(word => wordIds.Contains(word.WordId))
            .Select(word => new WordRow(
                word.WordId,
                word.Word1,
                word.PhoneticUk,
                word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                word.ImageUrl,
                word.CefrLevel))
            .ToListAsync(cancellationToken);
    }

    private sealed record WordRow(
        uint WordId,
        string Word,
        string? PhoneticUk,
        string? PrimaryMeaning,
        string? ImageUrl,
        string? CefrLevel);
}
