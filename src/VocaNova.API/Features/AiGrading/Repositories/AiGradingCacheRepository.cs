using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.AiGrading.Repositories;

public sealed class AiGradingCacheRepository : IAiGradingCacheRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AiGradingCacheRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CachedAiGradingResult?> FindValidAndIncrementHitAsync(
        string cacheKey,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var cache = await _dbContext.AiGradingCaches
            .SingleOrDefaultAsync(
                entity => entity.CacheKey == cacheKey && entity.ExpiresAt > now,
                cancellationToken);
        if (cache is null)
        {
            return null;
        }

        cache.HitCount++;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CachedAiGradingResult(
            cache.AiScore,
            cache.AiExplanation,
            cache.AiSuggestion);
    }
}
