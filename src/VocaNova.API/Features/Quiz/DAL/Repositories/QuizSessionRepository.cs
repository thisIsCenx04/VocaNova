using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class QuizSessionRepository : IQuizSessionRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public QuizSessionRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<QuizSession> CreateAsync(uint userId, CreateQuizSessionCommand command,
        IReadOnlyCollection<uint> topicIds, int questionCount,
        CancellationToken cancellationToken = default)
    {
        var session = new TestSession
        {
            UserId = userId,
            TestType = command.AnswerMethod!,
            Mode = command.Mode!,
            QuestionType = command.QuestionType,
            ScopeType = command.ScopeType!,
            ScopeDateFrom = command.ScopeDateFrom,
            ScopeDateTo = command.ScopeDateTo,
            WordOrder = command.WordOrder!,
            WordLimit = command.WordLimit,
            TimeLimitSec = command.TimeLimitSec,
            Lives = command.Lives,
            QuestionCount = questionCount,
            StartedAt = DateTime.UtcNow,
            Status = TestSessionStatus.InProgress,
        };
        foreach (var topicId in topicIds)
            session.TestSessionTopics.Add(new TestSessionTopic { TopicId = topicId });
        _dbContext.TestSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return session.ToBusinessSession(command.ListId);
    }
}
