using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Quiz.DTOs;
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

        RecalculateSessionStats(session);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return answer;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void RecalculateSessionStats(TestSession session)
    {
        var gradedAnswers = session.TestAnswers
            .Where(answer => answer.IsCorrect.HasValue)
            .OrderBy(answer => answer.QuestionNumber)
            .ThenBy(answer => answer.AnswerId)
            .ToArray();

        session.CorrectCount = gradedAnswers.Count(answer => answer.IsCorrect == true);
        session.WrongCount = gradedAnswers.Count(answer => answer.IsCorrect == false);
        session.Score = gradedAnswers.Length == 0
            ? 0
            : (float)session.CorrectCount / gradedAnswers.Length * 100;

        var currentStreak = 0;
        var maxStreak = 0;
        foreach (var answer in gradedAnswers)
        {
            if (answer.IsCorrect == true)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        session.MaxStreak = maxStreak;
    }
}
