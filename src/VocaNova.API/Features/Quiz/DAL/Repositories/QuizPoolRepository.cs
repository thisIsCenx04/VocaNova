using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizPoolRepository : IQuizPoolRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public QuizPoolRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<QuizPoolWord>> GetCandidatesAsync(
        uint userId, BuildQuizPoolCommand command, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserListWords.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == UserStatus.Active
                && item.List.Status == UserStatus.Active && item.Word.Status == UserStatus.Active);
        if (command.ListId is > 0) query = query.Where(item => item.ListId == command.ListId.Value);

        var addedFrom = command.ScopeDateFrom?.ToDateTime(TimeOnly.MinValue);
        var addedBefore = command.ScopeDateTo?.AddDays(1).ToDateTime(TimeOnly.MinValue);
        if (addedFrom.HasValue) query = query.Where(item => item.AddedAt >= addedFrom.Value);
        if (addedBefore.HasValue) query = query.Where(item => item.AddedAt < addedBefore.Value);
        if (command.TopicIds is { Count: > 0 })
            query = query.Where(item => item.Word.WordTopics.Any(topic => command.TopicIds.Contains(topic.TopicId)));
        if (command.ScopeType == ScopeType.WrongWords)
            query = query.Where(item => _dbContext.UserWordProgresses.Any(progress =>
                progress.UserId == userId && progress.WordId == item.WordId && progress.IsInWrongList));

        return await query.GroupBy(item => item.WordId)
            .Select(group => new QuizPoolWord(group.Key, group.Max(item => item.AddedAt),
                _dbContext.UserWordProgresses.Where(progress => progress.UserId == userId
                        && progress.WordId == group.Key)
                    .Select(progress => progress.WrongCount).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
