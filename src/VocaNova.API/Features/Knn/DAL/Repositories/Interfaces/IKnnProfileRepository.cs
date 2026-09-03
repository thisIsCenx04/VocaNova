using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IKnnProfileRepository
{
    Task<KnnLearningProfile?> GetLearningProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicPreference>> GetActiveTopicPreferencesAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<KnnProfileVectorSource?> GetProfileVectorSourceAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<KnnLookupDimensions> GetActiveLookupDimensionsAsync(
        CancellationToken cancellationToken = default);

    Task<LearningProfileOptions> GetActiveLookupOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnProfileVectorSource>> GetCandidateProfileSourcesAsync(
        uint excludingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetActiveTopicIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicPreference>> GetNeighborTopicPreferencesAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlySet<string> sources,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NeighborPersonalTopic>> GetNeighborPersonalTopicsAsync(
        uint currentUserId,
        IReadOnlyCollection<uint> neighborUserIds,
        int wordsPerTopic,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TopicRecommendation>> GetFallbackTopicRecommendationsAsync(
        IReadOnlyCollection<uint> excludedTopicIds,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TopicRecommendation>> GetTopicRecommendationsByScoreAsync(
        IReadOnlyDictionary<uint, double> scoresByTopicId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> UpsertTopicPreferenceAsync(
        uint userId,
        uint topicId,
        string source,
        DateTime now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the user's onboarding topic picks with <paramref name="topicIds"/> and returns
    /// the number stored, or <c>null</c> when any id does not exist.
    /// </summary>
    Task<int?> ReplaceOnboardingTopicPreferencesAsync(
        uint userId,
        IReadOnlyCollection<uint> topicIds,
        DateTime now,
        CancellationToken cancellationToken = default);
}
