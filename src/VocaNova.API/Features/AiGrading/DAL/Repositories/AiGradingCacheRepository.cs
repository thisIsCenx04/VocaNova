using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.AiGrading.BLL.Abstractions;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Features.AiGrading.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.AiGrading.DAL.Repositories;

public sealed class AiGradingCacheRepository : IAiGradingCacheRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public AiGradingCacheRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<CachedAiGrade?> FindValidAndRecordHitAsync(
        AiGradeCacheKey key, DateTime now, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AiGradingCaches.SingleOrDefaultAsync(
            item => item.CacheKey == key.Value && item.ExpiresAt > now, cancellationToken);
        if (entity is null) return null;
        entity.HitCount++;
        // Intentionally independent from the later Quiz submission save.
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToBusinessModel();
    }

    public async Task SaveAsync(CachedAiGrade result, CancellationToken cancellationToken = default)
    {
        var key = result.Key ?? throw new ArgumentException("A cache key is required.", nameof(result));
        var entity = await _dbContext.AiGradingCaches.SingleOrDefaultAsync(
            item => item.CacheKey == key.Value, cancellationToken);
        if (entity is null)
        {
            entity = new AiGradingCache { CacheKey = key.Value };
            _dbContext.AiGradingCaches.Add(entity);
        }
        entity.WordId = key.WordId;
        entity.QuestionType = key.QuestionType;
        entity.UserAnswerNormalized = key.NormalizedUserAnswer;
        entity.ExpectedAnswer = key.ExpectedAnswer;
        entity.AiScore = result.Score;
        entity.AiExplanation = result.Explanation;
        entity.AiSuggestion = result.Suggestion;
        entity.HitCount = result.HitCount;
        entity.CreatedAt = result.CreatedAt ?? DateTime.UtcNow;
        entity.ExpiresAt = result.ExpiresAt ?? DateTime.UtcNow.AddDays(7);
        // Intentionally independent from the later Quiz submission save.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
