using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Features.AiGrading.BLL.Abstractions;

public interface IAiGradingCacheRepository
{
    Task<CachedAiGrade?> FindValidAndRecordHitAsync(
        AiGradeCacheKey key, DateTime now, CancellationToken cancellationToken = default);
    Task SaveAsync(CachedAiGrade result, CancellationToken cancellationToken = default);
}
