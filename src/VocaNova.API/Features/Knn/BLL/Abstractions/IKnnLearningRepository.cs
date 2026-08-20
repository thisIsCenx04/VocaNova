using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IKnnLearningRepository
{
    Task<int> GetSessionCountAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<uint>> GetActiveTopicIdsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicAnswerStatistics>> GetTopicAnswerStatsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetEligibleUserIdsAsync(
        int minSessions,
        uint excludingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnMasteredWord>> GetMasteredWordsAsync(
        IReadOnlyCollection<uint> userIds,
        int minMasteryLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Words the given neighbours are actually studying, optionally narrowed to
    /// <paramref name="topicIds"/> (pass an empty collection to skip the topic filter). This is
    /// the cold-start counterpart of <see cref="GetMasteredWordsAsync"/>: a new user's neighbours
    /// are matched on profile, so requiring proven mastery would return far too little.
    /// </summary>
    Task<IReadOnlyCollection<KnnNeighborWord>> GetNeighborStudiedWordsAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetActiveListWordIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WordRecommendationItem>> GetWordRecommendationItemsAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WordRecommendation>> GetWordRecommendationsAsync(
        IReadOnlyDictionary<uint, double> scoresByWordId,
        int limit,
        CancellationToken cancellationToken = default);
}
