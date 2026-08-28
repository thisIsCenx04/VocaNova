using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IKnnTopicRecommendationCache
{
    Task<IReadOnlyCollection<TopicRecommendation>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        IReadOnlyCollection<TopicRecommendation> recommendations,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
