using VocaNova.API.Features.AiGrading.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.AiGrading.Repositories;

public interface IAiGradingCacheRepository
{
    Task<CachedAiGradingResult?> FindValidAndIncrementHitAsync(
        string cacheKey,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AiGradingCache cache,
        CancellationToken cancellationToken = default);
}
