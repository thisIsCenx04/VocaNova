using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizSessionRepository : IQuizSessionRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizSessionRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuizSessionDto> CreateAsync(
        uint userId,
        CreateSessionRequest request,
        int questionCount,
        CancellationToken cancellationToken = default)
    {
        var session = new TestSession
        {
            UserId = userId,
            TestType = request.AnswerMethod!,
            Mode = request.Mode!,
            QuestionType = request.QuestionType,
            ScopeType = request.ScopeType!,
            ScopeDateFrom = request.ScopeDateFrom,
            ScopeDateTo = request.ScopeDateTo,
            WordOrder = request.WordOrder!,
            WordLimit = request.WordLimit,
            TimeLimitSec = request.TimeLimitSec,
            Lives = request.Lives,
            QuestionCount = questionCount,
            CorrectCount = 0,
            WrongCount = 0,
            Score = 0,
            MaxStreak = 0,
            StartedAt = DateTime.UtcNow,
            Status = TestSessionStatus.InProgress,
        };

        foreach (var topicId in NormalizeTopicIds(request.TopicIds))
        {
            session.TestSessionTopics.Add(new TestSessionTopic
            {
                TopicId = topicId,
            });
        }

        _dbContext.TestSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // list_id is not persisted (no schema change); echo it back so the
        // client can carry it on subsequent answer requests to keep scoping.
        return ToDto(session, request.ListId);
    }

    private static QuizSessionDto ToDto(TestSession session, uint? listId)
    {
        return new QuizSessionDto(
            session.SessionId,
            session.TestType,
            session.Mode,
            session.QuestionType,
            session.ScopeType,
            session.ScopeDateFrom,
            session.ScopeDateTo,
            session.WordOrder,
            session.WordLimit,
            session.TimeLimitSec,
            session.Lives,
            session.QuestionCount,
            session.Status,
            session.StartedAt,
            session.TestSessionTopics
                .Select(topic => topic.TopicId)
                .OrderBy(topicId => topicId)
                .ToArray(),
            listId);
    }

    private static IReadOnlyCollection<uint> NormalizeTopicIds(IReadOnlyCollection<uint>? topicIds)
    {
        return topicIds?
            .Where(topicId => topicId > 0)
            .Distinct()
            .ToArray()
            ?? Array.Empty<uint>();
    }
}
