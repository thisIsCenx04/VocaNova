using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Services.IServices;

namespace VocaNova.API.Features.Knn.BLL.Services;

public sealed class KnnOnboardingService : IKnnOnboardingService
{
    private readonly IKnnProfileRepository _knnProfileRepository;
    private readonly IKnnTopicRecommendationCache? _recommendationCache;
    private readonly IKnnRuntimeConfigurationService? _runtimeConfig;
    private readonly KnnOptions _options;

    public KnnOnboardingService(
        IKnnProfileRepository knnProfileRepository,
        IOptions<KnnOptions> options,
        IKnnTopicRecommendationCache? recommendationCache = null,
        IKnnRuntimeConfigurationService? runtimeConfig = null)
    {
        _knnProfileRepository = knnProfileRepository;
        _recommendationCache = recommendationCache;
        _runtimeConfig = runtimeConfig;
        _options = options.Value;
    }

    public async Task<double[]> ComputeProfileVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _knnProfileRepository.GetProfileVectorSourceAsync(userId, cancellationToken);
        if (profile is null)
        {
            return Array.Empty<double>();
        }

        var dimensions = await _knnProfileRepository.GetActiveLookupDimensionsAsync(cancellationToken);
        var weights = await GetVectorWeightsAsync(cancellationToken);
        return BuildProfileVector(profile, dimensions, weights);
    }

    public double CosineSimilarity(double[] a, double[] b)
    {
        return KnnMath.CosineSimilarity(a, b);
    }

    public async Task<KnnOperationResult<LearningProfileOptions>> GetLearningProfileOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var options = await _knnProfileRepository.GetActiveLookupOptionsAsync(cancellationToken);
        return KnnOperationResult<LearningProfileOptions>.Success(options);
    }

    public async Task<KnnOperationResult<int>> SelectTopicsAsync(
        uint userId,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<int>.Unauthorized("Unauthorized.");
        }

        if (topicIds.Count > AppSettings.MaxPageLimit)
        {
            return KnnOperationResult<int>.ValidationFailure($"At most {AppSettings.MaxPageLimit} topics can be selected.");
        }

        var storedCount = await _knnProfileRepository.ReplaceOnboardingTopicPreferencesAsync(
            userId,
            topicIds,
            DateTime.UtcNow,
            cancellationToken);
        if (storedCount is null)
        {
            return KnnOperationResult<int>.NotFound("One or more topics were not found.");
        }

        // The picks are part of the profile vector, so any cached recommendation is now stale.
        if (_recommendationCache is not null)
        {
            await _recommendationCache.RemoveAsync(userId, cancellationToken);
        }

        return KnnOperationResult<int>.Success(storedCount.Value);
    }

    public async Task<KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>> RecommendTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = NormalizeLimit(limit);
        if (normalizedLimit is null)
        {
            return KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>.ValidationFailure(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cachedRecommendations = _recommendationCache is null
            ? null
            : await _recommendationCache.GetAsync(userId, cancellationToken);
        if (cachedRecommendations is not null)
        {
            return KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>.Success(
                cachedRecommendations.Take(normalizedLimit.Value).ToArray());
        }

        var profile = await _knnProfileRepository.GetProfileVectorSourceAsync(userId, cancellationToken);
        var excludedTopicIds = await _knnProfileRepository.GetActiveTopicIdsAsync(userId, cancellationToken);

        // A user who answered nothing at all still deserves the popular-topic fallback rather
        // than an empty list.
        if (profile is null)
        {
            var fallback = await _knnProfileRepository.GetFallbackTopicRecommendationsAsync(
                excludedTopicIds,
                normalizedLimit.Value,
                cancellationToken);
            return KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>.Success(fallback);
        }

        var dimensions = await _knnProfileRepository.GetActiveLookupDimensionsAsync(cancellationToken);
        var weights = await GetVectorWeightsAsync(cancellationToken);
        var userVector = BuildProfileVector(profile, dimensions, weights);

        var recommendations = KnnProfileVectorBuilder.IsZeroVector(userVector)
            ? await _knnProfileRepository.GetFallbackTopicRecommendationsAsync(
                excludedTopicIds,
                AppSettings.MaxPageLimit,
                cancellationToken)
            : await BuildKnnRecommendationsAsync(
                userId,
                userVector,
                dimensions,
                weights,
                excludedTopicIds,
                cancellationToken);

        var orderedRecommendations = recommendations
            .OrderByDescending(topic => topic.RecommendationScore)
            .ThenBy(topic => topic.TopicName)
            .ThenBy(topic => topic.TopicId)
            .Take(AppSettings.MaxPageLimit)
            .ToArray();

        if (_recommendationCache is not null)
        {
            await _recommendationCache.SetAsync(
                userId,
                orderedRecommendations,
                TimeSpan.FromMinutes(_options.Onboarding.CacheTtlMinutes),
                cancellationToken);
        }

        return KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>.Success(
            orderedRecommendations.Take(normalizedLimit.Value).ToArray());
    }

    public async Task<KnnOperationResult<bool>> AcceptTopicAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<bool>.Unauthorized("Unauthorized.");
        }

        var accepted = await _knnProfileRepository.UpsertTopicPreferenceAsync(
            userId,
            topicId,
            TopicPreferenceSource.KnnSuggested,
            DateTime.UtcNow,
            cancellationToken);
        if (!accepted)
        {
            return KnnOperationResult<bool>.NotFound("Topic not found.");
        }

        if (_recommendationCache is not null)
        {
            await _recommendationCache.RemoveAsync(userId, cancellationToken);
        }

        return KnnOperationResult<bool>.Success(true);
    }

    public async Task<KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>> RecommendPersonalTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = NormalizeLimit(limit);
        if (normalizedLimit is null)
        {
            return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.ValidationFailure(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var profile = await _knnProfileRepository.GetProfileVectorSourceAsync(userId, cancellationToken);
        if (profile is null)
        {
            return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.Success(
                Array.Empty<PersonalTopicRecommendation>());
        }

        var dimensions = await _knnProfileRepository.GetActiveLookupDimensionsAsync(cancellationToken);
        var weights = await GetVectorWeightsAsync(cancellationToken);
        var userVector = BuildProfileVector(profile, dimensions, weights);
        if (KnnProfileVectorBuilder.IsZeroVector(userVector))
        {
            return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.Success(
                Array.Empty<PersonalTopicRecommendation>());
        }

        var neighbors = (await _knnProfileRepository.GetCandidateProfileSourcesAsync(
                userId,
                cancellationToken))
            .Select(candidate => new
            {
                candidate.UserId,
                Similarity = KnnMath.CosineSimilarity(
                    userVector,
                    BuildProfileVector(candidate, dimensions, weights)),
            })
            .Where(candidate => candidate.Similarity >= _options.Onboarding.MinSimilarity)
            .OrderByDescending(candidate => candidate.Similarity)
            .ThenBy(candidate => candidate.UserId)
            .Take(_options.Onboarding.KValue)
            .ToArray();
        if (neighbors.Length == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.Success(
                Array.Empty<PersonalTopicRecommendation>());
        }

        var similarityByUserId = neighbors.ToDictionary(item => item.UserId, item => item.Similarity);
        var personalTopics = await _knnProfileRepository.GetNeighborPersonalTopicsAsync(
            userId,
            similarityByUserId.Keys.ToArray(),
            4,
            cancellationToken);

        var recommendations = personalTopics
            .GroupBy(topic => topic.TopicId)
            .Select(group =>
            {
                var first = group.First();
                var words = group
                    .OrderByDescending(topic => similarityByUserId[topic.OwnerUserId])
                    .SelectMany(topic => topic.Words)
                    .DistinctBy(word => word.WordId)
                    .Take(4)
                    .ToArray();
                return new PersonalTopicRecommendation(
                    first.TopicId,
                    first.Name,
                    first.NameVi,
                    first.Icon,
                    group.Sum(topic => topic.WordCount),
                    group.Sum(topic => similarityByUserId[topic.OwnerUserId]),
                    words);
            })
            .Where(topic => topic.Words.Count > 0)
            .OrderByDescending(topic => topic.RecommendationScore)
            .ThenBy(topic => topic.Name)
            .Take(normalizedLimit.Value)
            .ToArray();

        return KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>.Success(recommendations);
    }

    private async Task<IReadOnlyCollection<TopicRecommendation>> BuildKnnRecommendationsAsync(
        uint userId,
        double[] userVector,
        KnnLookupDimensions dimensions,
        KnnVectorOptions weights,
        IReadOnlyCollection<uint> excludedTopicIds,
        CancellationToken cancellationToken)
    {
        var candidateProfiles = await _knnProfileRepository.GetCandidateProfileSourcesAsync(
            userId,
            cancellationToken);
        var nearestNeighbors = candidateProfiles
            .Select(profile => new
            {
                profile.UserId,
                Similarity = KnnMath.CosineSimilarity(
                    userVector,
                    BuildProfileVector(profile, dimensions, weights)),
            })
            .Where(neighbor => neighbor.Similarity >= _options.Onboarding.MinSimilarity)
            .OrderByDescending(neighbor => neighbor.Similarity)
            .ThenBy(neighbor => neighbor.UserId)
            .Take(_options.Onboarding.KValue)
            .ToArray();

        if (nearestNeighbors.Length == 0)
        {
            return await _knnProfileRepository.GetFallbackTopicRecommendationsAsync(
                excludedTopicIds,
                AppSettings.MaxPageLimit,
                cancellationToken);
        }

        var similarityByUserId = nearestNeighbors.ToDictionary(
            neighbor => neighbor.UserId,
            neighbor => neighbor.Similarity);
        var neighborTopicPreferences = await _knnProfileRepository.GetNeighborTopicPreferencesAsync(
            similarityByUserId.Keys.ToArray(),
            TopicPreferenceSource.NeighborSources,
            cancellationToken);

        var scoresByTopicId = neighborTopicPreferences
            .Where(preference => !excludedTopicIds.Contains(preference.TopicId))
            .GroupBy(preference => preference.TopicId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(preference => similarityByUserId[preference.UserId]));
        if (scoresByTopicId.Count == 0)
        {
            return await _knnProfileRepository.GetFallbackTopicRecommendationsAsync(
                excludedTopicIds,
                AppSettings.MaxPageLimit,
                cancellationToken);
        }

        return await _knnProfileRepository.GetTopicRecommendationsByScoreAsync(
            scoresByTopicId,
            AppSettings.MaxPageLimit,
            cancellationToken);
    }

    private int? NormalizeLimit(int? limit)
    {
        var normalized = limit ?? _options.Onboarding.DefaultTopicLimit;
        return normalized <= 0 || normalized > AppSettings.MaxPageLimit
            ? null
            : normalized;
    }

    /// <summary>
    /// Weights an admin tuned from the dashboard win over the deployment configuration.
    /// </summary>
    private async Task<KnnVectorOptions> GetVectorWeightsAsync(CancellationToken cancellationToken)
    {
        if (_runtimeConfig is null)
        {
            return _options.Vector;
        }

        return await _runtimeConfig.GetVectorOptionsAsync(cancellationToken);
    }

    private static double[] BuildProfileVector(
        KnnProfileVectorSource profile,
        KnnLookupDimensions dimensions,
        KnnVectorOptions weights)
    {
        return KnnProfileVectorBuilder.Build(profile, dimensions, weights);
    }
}
