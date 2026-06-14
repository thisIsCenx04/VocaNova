using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IKnnTopicRecommendationCache
{
    Task<IReadOnlyCollection<TopicRecommendationDto>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        IReadOnlyCollection<TopicRecommendationDto> recommendations,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
