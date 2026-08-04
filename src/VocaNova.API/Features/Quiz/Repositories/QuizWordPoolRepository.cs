using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizWordPoolRepository : IQuizWordPoolRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizWordPoolRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<QuizPoolWordDto>> GetCandidatesAsync(
        uint userId,
        DateTime? addedFrom,
        DateTime? addedBefore,
        IReadOnlyCollection<uint>? topicIds,
        uint? listId,
        bool wrongWordsOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.Status == UserStatus.Active
                && listWord.List.Status == UserStatus.Active
                && listWord.Word.Status == UserStatus.Active);

        if (listId is > 0)
        {
            query = query.Where(listWord => listWord.ListId == listId.Value);
        }

        if (addedFrom.HasValue)
        {
            query = query.Where(listWord => listWord.AddedAt >= addedFrom.Value);
        }

        if (addedBefore.HasValue)
        {
            query = query.Where(listWord => listWord.AddedAt < addedBefore.Value);
        }

        if (topicIds is { Count: > 0 })
        {
            query = query.Where(listWord => listWord.Word.WordTopics
                .Any(wordTopic => topicIds.Contains(wordTopic.TopicId)));
        }

        if (wrongWordsOnly)
        {
            query = query.Where(listWord => _dbContext.UserWordProgresses
                .Any(progress => progress.UserId == userId
                    && progress.WordId == listWord.WordId
                    && progress.IsInWrongList));
        }

        return await query
            .GroupBy(listWord => listWord.WordId)
            .Select(group => new QuizPoolWordDto(
                group.Key,
                group.Max(listWord => listWord.AddedAt),
                _dbContext.UserWordProgresses
                    .Where(progress => progress.UserId == userId
                        && progress.WordId == group.Key)
                    .Select(progress => progress.WrongCount)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
