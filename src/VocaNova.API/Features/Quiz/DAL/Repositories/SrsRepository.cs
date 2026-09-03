using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Quiz.BLL.Abstractions;
using VocaNova.API.Features.Quiz.BLL.Models;
using VocaNova.API.Features.Quiz.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using PersistenceProgress = VocaNova.API.Infrastructure.Persistence.Entities.UserWordProgress;

namespace VocaNova.API.Features.Quiz.DAL.Repositories;

public sealed class SrsRepository : ISrsRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public SrsRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<UserWordProgress?> FindAsync(uint userId, uint wordId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserWordProgresses.SingleOrDefaultAsync(
            item => item.UserId == userId && item.WordId == wordId, cancellationToken);
        return entity?.ToBusinessProgress();
    }

    public void Stage(UserWordProgress progress)
    {
        var entity = _dbContext.UserWordProgresses.Local.SingleOrDefault(item =>
            item.UserId == progress.UserId && item.WordId == progress.WordId);
        if (entity is null)
        {
            entity = new PersistenceProgress
            {
                ProgressId = progress.ProgressId,
                UserId = progress.UserId,
                WordId = progress.WordId,
            };
            _dbContext.UserWordProgresses.Add(entity);
        }
        progress.Apply(entity);
    }
}
