using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizHistoryRepository : IQuizHistoryRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public QuizHistoryRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedCollection<QuizHistoryItem>> GetHistoryAsync(
        uint userId, int page, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TestSessions.Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt).ThenByDescending(session => session.SessionId)
            .Select(session => new QuizHistoryItem(session.SessionId, session.TestType, session.Mode,
                session.QuestionType, session.QuestionCount, session.CorrectCount, session.WrongCount,
                session.CorrectCount + session.WrongCount == 0 ? 0
                    : (float)session.CorrectCount / (session.CorrectCount + session.WrongCount) * 100,
                session.Score, session.MaxStreak, session.Status, session.StartedAt, session.EndedAt));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);
        return new PagedCollection<QuizHistoryItem>(items, page, limit, total);
    }

    public async Task<PagedCollection<WrongWord>> GetWrongWordsAsync(
        uint userId, int page, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserWordProgresses
            .Where(progress => progress.UserId == userId && progress.IsInWrongList)
            .OrderByDescending(progress => progress.WrongCount)
            .ThenByDescending(progress => progress.LastWrongAt).ThenBy(progress => progress.Word.Word1)
            .Select(progress => new WrongWord(progress.WordId, progress.Word.Word1,
                progress.Word.WordSenses.OrderBy(sense => sense.SenseOrder).ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning).FirstOrDefault(),
                progress.TestCount, progress.CorrectCount, progress.WrongCount, progress.MasteryLevel,
                progress.LastWrongAt, progress.NextReviewAt));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);
        return new PagedCollection<WrongWord>(items, page, limit, total);
    }

    public async Task<bool> ClearWrongWordAsync(uint userId, uint wordId,
        CancellationToken cancellationToken = default)
    {
        var progress = await _dbContext.UserWordProgresses.SingleOrDefaultAsync(item =>
            item.UserId == userId && item.WordId == wordId && item.IsInWrongList, cancellationToken);
        if (progress is null) return false;
        progress.IsInWrongList = false;
        progress.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
