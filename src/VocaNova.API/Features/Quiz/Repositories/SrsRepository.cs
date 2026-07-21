using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Quiz.Repositories;

public sealed class SrsRepository : ISrsRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public SrsRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserWordProgress?> FindAsync(
        uint userId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserWordProgresses
            .SingleOrDefaultAsync(
                progress => progress.UserId == userId && progress.WordId == wordId,
                cancellationToken);
    }

    public void Add(UserWordProgress progress)
    {
        _dbContext.UserWordProgresses.Add(progress);
    }
}
