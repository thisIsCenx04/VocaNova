using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizResultRepository : IQuizResultRepository
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly Dictionary<uint, TestSession> _loadedSessions = [];
    public QuizResultRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<QuizResultState?> GetSessionAsync(uint userId, uint sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.TestSessions.SingleOrDefaultAsync(
            item => item.SessionId == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        _loadedSessions[session.SessionId] = session;
        return new QuizResultState
        {
            SessionId = session.SessionId,
            Status = session.Status,
            QuestionCount = session.QuestionCount,
            CorrectCount = session.CorrectCount,
            WrongCount = session.WrongCount,
            Score = session.Score,
            MaxStreak = session.MaxStreak,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
        };
    }

    public async Task SaveFinishAsync(QuizFinishChanges changes,
        CancellationToken cancellationToken = default)
    {
        var entity = _loadedSessions.GetValueOrDefault(changes.Session.SessionId)
            ?? await _dbContext.TestSessions.SingleAsync(
                item => item.SessionId == changes.Session.SessionId, cancellationToken);
        entity.Status = changes.Session.Status;
        entity.CorrectCount = changes.Session.CorrectCount;
        entity.WrongCount = changes.Session.WrongCount;
        entity.Score = changes.Session.Score;
        entity.MaxStreak = changes.Session.MaxStreak;
        entity.EndedAt = changes.Session.EndedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TestAnswerResult>> GetAnswersAsync(
        uint sessionId, CancellationToken cancellationToken = default) =>
        await _dbContext.TestAnswers.AsNoTracking().Where(answer => answer.SessionId == sessionId)
            .OrderBy(answer => answer.QuestionNumber).ThenBy(answer => answer.AnswerId)
            .Select(answer => new TestAnswerResult(answer.AnswerId, answer.WordId, answer.SenseId,
                answer.QuestionNumber, answer.QuestionType, answer.DisplayContent, answer.ExpectedAnswer,
                answer.UserAnswer, answer.IsCorrect, answer.AiScore, answer.AiExplanation, answer.AiSuggestion))
            .ToListAsync(cancellationToken);
}
