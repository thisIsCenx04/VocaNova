using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizHistoryRepository : IQuizHistoryRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizHistoryRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<QuizHistoryItemDto>> GetHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TestSessions
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.SessionId)
            .Select(session => new QuizHistoryItemDto(
                session.SessionId,
                session.TestType,
                session.Mode,
                session.QuestionType,
                session.QuestionCount,
                session.CorrectCount,
                session.WrongCount,
                session.CorrectCount + session.WrongCount == 0
                    ? 0
                    : (float)session.CorrectCount / (session.CorrectCount + session.WrongCount) * 100,
                session.Score,
                session.MaxStreak,
                session.Status,
                session.StartedAt,
                session.EndedAt))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public Task<PagedResult<WrongWordDto>> GetWrongWordsAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.IsInWrongList)
            .OrderByDescending(progress => progress.WrongCount)
            .ThenByDescending(progress => progress.LastWrongAt)
            .ThenBy(progress => progress.Word.Word1)
            .Select(progress => new WrongWordDto(
                progress.WordId,
                progress.Word.Word1,
                progress.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                progress.TestCount,
                progress.CorrectCount,
                progress.WrongCount,
                progress.MasteryLevel,
                progress.LastWrongAt,
                progress.NextReviewAt))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public async Task<bool> ClearWrongWordAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var progress = await _dbContext.UserWordProgresses
            .SingleOrDefaultAsync(
                entity => entity.UserId == userId
                    && entity.WordId == wordId
                    && entity.IsInWrongList,
                cancellationToken);
        if (progress is null)
        {
            return false;
        }

        progress.IsInWrongList = false;
        progress.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
