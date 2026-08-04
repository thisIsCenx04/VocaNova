using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class QuizResultRepository : IQuizResultRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public QuizResultRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TestSession?> FindSessionAsync(
        uint userId,
        uint sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TestSessions
            .Include(session => session.TestAnswers)
            .SingleOrDefaultAsync(
                session => session.SessionId == sessionId && session.UserId == userId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
