using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Repositories;

public interface IKnnProfileRepository
{
    Task<KnnLearningProfileDto?> GetLearningProfileAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicPreferenceDto>> GetActiveTopicPreferencesAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<KnnProfileVectorSourceDto?> GetProfileVectorSourceAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<KnnLookupDimensionsDto> GetActiveLookupDimensionsAsync(
        CancellationToken cancellationToken = default);

    Task<LearningProfileOptionsDto> GetActiveLookupOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnProfileVectorSourceDto>> GetCandidateProfileSourcesAsync(
        uint excludingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetActiveTopicIdsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnTopicPreferenceDto>> GetNeighborTopicPreferencesAsync(
        IReadOnlyCollection<uint> userIds,
        IReadOnlySet<string> sources,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NeighborPersonalTopicDto>> GetNeighborPersonalTopicsAsync(
        uint currentUserId,
        IReadOnlyCollection<uint> neighborUserIds,
        int wordsPerTopic,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TopicRecommendationDto>> GetFallbackTopicRecommendationsAsync(
        IReadOnlyCollection<uint> excludedTopicIds,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TopicRecommendationDto>> GetTopicRecommendationsByScoreAsync(
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
