using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizSubmissionRepository : IQuizSubmissionRepository
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly Dictionary<uint, TestSession> _loadedSessions = [];

    public QuizSubmissionRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<QuizSubmissionState?> GetStateAsync(uint userId, uint sessionId, uint wordId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TestSessions
            .Include(session => session.TestSessionTopics)
            .Include(session => session.TestAnswers)
            .SingleOrDefaultAsync(session => session.SessionId == sessionId && session.UserId == userId,
                cancellationToken);
        if (entity is null) return null;
        _loadedSessions[entity.SessionId] = entity;
        return entity.ToSubmissionState();
    }

    public async Task SaveSubmissionAsync(QuizSubmissionChanges changes,
        CancellationToken cancellationToken = default)
    {
        if (!_loadedSessions.TryGetValue(changes.Session.SessionId, out var session))
        {
            session = await _dbContext.TestSessions.Include(item => item.TestAnswers)
                .SingleAsync(item => item.SessionId == changes.Session.SessionId, cancellationToken);
        }

        var answer = session.TestAnswers.SingleOrDefault(item => item.WordId == changes.Answer.WordId);
        if (answer is null)
        {
            answer = new TestAnswer
            {
                SessionId = session.SessionId,
                WordId = changes.Answer.WordId,
                QuestionNumber = changes.Answer.QuestionNumber,
            };
            session.TestAnswers.Add(answer);
            _dbContext.TestAnswers.Add(answer);
        }
        answer.SenseId = changes.Answer.SenseId;
        answer.QuestionType = changes.Answer.QuestionType;
        answer.DisplayContent = changes.Answer.DisplayContent;
        answer.ExpectedAnswer = changes.Answer.ExpectedAnswer;
        answer.UserAnswer = changes.Answer.UserAnswer;
        answer.IsCorrect = changes.Answer.IsCorrect;
        answer.AiScore = changes.Answer.AiScore;
        answer.AiExplanation = changes.Answer.AiExplanation;
        answer.AiSuggestion = changes.Answer.AiSuggestion;

        session.CorrectCount = changes.Session.CorrectCount;
        session.WrongCount = changes.Session.WrongCount;
        session.Score = changes.Session.Score;
        session.MaxStreak = changes.Session.MaxStreak;
        session.Status = changes.Session.Status;
        session.EndedAt = changes.Session.EndedAt;

        var progress = _dbContext.UserWordProgresses.Local.SingleOrDefault(item =>
            item.UserId == changes.Progress.UserId && item.WordId == changes.Progress.WordId);
        if (progress is null)
        {
            progress = new VocaNova.API.Infrastructure.Persistence.Entities.UserWordProgress
            {
                ProgressId = changes.Progress.ProgressId,
                UserId = changes.Progress.UserId,
                WordId = changes.Progress.WordId,
            };
            _dbContext.UserWordProgresses.Add(progress);
        }
        changes.Progress.Apply(progress);

        // Exactly one relational save: answer, session and SRS progress share this call.
        await _dbContext.SaveChangesAsync(cancellationToken);
        changes.Answer.AnswerId = answer.AnswerId;
        changes.Progress.ProgressId = progress.ProgressId;
    }
}
