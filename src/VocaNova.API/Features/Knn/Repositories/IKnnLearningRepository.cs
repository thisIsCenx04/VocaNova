using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Repositories;

public interface IKnnLearningRepository
{
    Task<int> GetSessionCountAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<uint>> GetActiveTopicIdsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicAnswerStatsDto>> GetTopicAnswerStatsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetEligibleUserIdsAsync(
        int minSessions,
        uint excludingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnMasteredWordDto>> GetMasteredWordsAsync(
        IReadOnlyCollection<uint> userIds,
        int minMasteryLevel,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetActiveListWordIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WordRecommendationItem>> GetWordRecommendationItemsAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WordRecommendationDto>> GetWordRecommendationDtosAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default);
}
