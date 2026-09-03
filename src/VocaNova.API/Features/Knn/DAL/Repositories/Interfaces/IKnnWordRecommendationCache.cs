using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Abstractions;

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
