using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

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

    public async Task SaveAsync(
        AiGradingCache cache,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.AiGradingCaches
            .SingleOrDefaultAsync(
                entity => entity.CacheKey == cache.CacheKey,
                cancellationToken);

        if (existing is null)
        {
            _dbContext.AiGradingCaches.Add(cache);
        }
        else
        {
            existing.WordId = cache.WordId;
            existing.QuestionType = cache.QuestionType;
            existing.UserAnswerNormalized = cache.UserAnswerNormalized;
            existing.ExpectedAnswer = cache.ExpectedAnswer;
            existing.AiScore = cache.AiScore;
            existing.AiExplanation = cache.AiExplanation;
            existing.AiSuggestion = cache.AiSuggestion;
            existing.HitCount = 1;
            existing.CreatedAt = cache.CreatedAt;
            existing.ExpiresAt = cache.ExpiresAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
