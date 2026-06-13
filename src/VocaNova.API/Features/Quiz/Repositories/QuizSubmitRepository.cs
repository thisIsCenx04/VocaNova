using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Features.Quiz.Services;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizSubmitRepository : IQuizSubmitRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizSubmitRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TestSession?> FindSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TestSessions
            .Include(session => session.TestSessionTopics)
            .Include(session => session.TestAnswers)
            .SingleOrDefaultAsync(
                session => session.SessionId == sessionId && session.UserId == userId,
                cancellationToken);
    }

    public async Task<TestAnswer> UpsertAnswerAsync(
        TestSession session,
        QuestionDto question,
        SubmitAnswerRequest request,
        bool isCorrect,
        float? aiScore,
        string? aiExplanation,
        string? aiSuggestion,
        CancellationToken cancellationToken = default)
    {
        var answer = session.TestAnswers
            .SingleOrDefault(entity => entity.WordId == request.WordId);

        if (answer is null)
        {
            answer = new TestAnswer
            {
                SessionId = session.SessionId,
                WordId = request.WordId,
                QuestionNumber = session.TestAnswers.Count + 1,
            };

            session.TestAnswers.Add(answer);
            _dbContext.TestAnswers.Add(answer);
        }

        answer.SenseId = question.SenseId;
        answer.QuestionType = question.QuestionType;
        answer.DisplayContent = question.DisplayContent;
        answer.ExpectedAnswer = question.ExpectedAnswer;
        answer.UserAnswer = request.UserAnswer;
        answer.IsCorrect = isCorrect;
        answer.AiScore = aiScore;
        answer.AiExplanation = aiExplanation;
        answer.AiSuggestion = aiSuggestion;

        QuizSessionStatsCalculator.ApplyStats(session);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return answer;
    }

    public async Task CompleteSessionAsync(
        TestSession session,
        CancellationToken cancellationToken = default)
    {
        QuizSessionStatsCalculator.ApplyStats(session);
        session.Status = TestSessionStatus.Completed;
        session.EndedAt ??= DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
