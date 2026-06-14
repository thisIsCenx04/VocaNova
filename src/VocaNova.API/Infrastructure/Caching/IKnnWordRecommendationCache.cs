using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IKnnWordRecommendationCache
{
    Task<IReadOnlyCollection<WordRecommendationItem>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        IReadOnlyCollection<WordRecommendationItem> recommendations,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
