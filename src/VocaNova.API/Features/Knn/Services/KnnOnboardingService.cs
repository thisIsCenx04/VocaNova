using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Knn.Services;

public sealed class KnnOnboardingService : IKnnOnboardingService
{
    private readonly IKnnProfileRepository _knnProfileRepository;
    private readonly IKnnTopicRecommendationCache? _recommendationCache;
    private readonly KnnOptions _options;

    public KnnOnboardingService(
        IKnnProfileRepository knnProfileRepository,
        IOptions<KnnOptions> options,
        IKnnTopicRecommendationCache? recommendationCache = null)
    {
        _knnProfileRepository = knnProfileRepository;
        _recommendationCache = recommendationCache;
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
        return BuildProfileVector(profile, dimensions);
    }

    public double CosineSimilarity(double[] a, double[] b)
    {
        return KnnMathHelper.CosineSimilarity(a, b);
    }

    public async Task<Result<IReadOnlyCollection<TopicRecommendationDto>>> RecommendTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<TopicRecommendationDto>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = NormalizeLimit(limit);
        if (normalizedLimit is null)
        {
            return Result<IReadOnlyCollection<TopicRecommendationDto>>.Fail(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cachedRecommendations = _recommendationCache is null
            ? null
            : await _recommendationCache.GetAsync(userId, cancellationToken);
        if (cachedRecommendations is not null)
        {
            return Result<IReadOnlyCollection<TopicRecommendationDto>>.Ok(
                cachedRecommendations.Take(normalizedLimit.Value).ToArray());
        }

        var profile = await _knnProfileRepository.GetProfileVectorSourceAsync(userId, cancellationToken);
        if (profile is null)
        {
            return Result<IReadOnlyCollection<TopicRecommendationDto>>.Ok(Array.Empty<TopicRecommendationDto>());
        }

        var dimensions = await _knnProfileRepository.GetActiveLookupDimensionsAsync(cancellationToken);
        var userVector = BuildProfileVector(profile, dimensions);
        var excludedTopicIds = await _knnProfileRepository.GetActiveTopicIdsAsync(userId, cancellationToken);

        var recommendations = IsZeroVector(userVector)
            ? await _knnProfileRepository.GetFallbackTopicRecommendationsAsync(
                excludedTopicIds,
                AppSettings.MaxPageLimit,
                cancellationToken)
            : await BuildKnnRecommendationsAsync(
                userId,
                userVector,
                dimensions,
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

        return Result<IReadOnlyCollection<TopicRecommendationDto>>.Ok(
            orderedRecommendations.Take(normalizedLimit.Value).ToArray());
    }

    public async Task<Result<bool>> AcceptTopicAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<bool>.Unauthorized("Unauthorized.");
        }

        var accepted = await _knnProfileRepository.UpsertTopicPreferenceAsync(
            userId,
            topicId,
            TopicPreferenceSource.KnnSuggested,
            DateTime.UtcNow,
            cancellationToken);
        if (!accepted)
        {
            return Result<bool>.NotFound("Topic not found.");
        }

        if (_recommendationCache is not null)
        {
            await _recommendationCache.RemoveAsync(userId, cancellationToken);
        }

        return Result<bool>.Ok(true);
    }

    private async Task<IReadOnlyCollection<TopicRecommendationDto>> BuildKnnRecommendationsAsync(
        uint userId,
        double[] userVector,
        KnnLookupDimensionsDto dimensions,
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
                Similarity = KnnMathHelper.CosineSimilarity(userVector, BuildProfileVector(profile, dimensions)),
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

    private static double[] BuildProfileVector(
        KnnProfileVectorSourceDto profile,
        KnnLookupDimensionsDto dimensions)
    {
        var values = new List<double>(
            dimensions.AgeRangeIds.Count
            + dimensions.RegionIds.Count
            + dimensions.OccupationIds.Count
            + dimensions.EducationLevelIds.Count
            + dimensions.LearningPurposeIds.Count);

        AppendOneHot(values, dimensions.AgeRangeIds, profile.AgeRangeId);
        AppendOneHot(values, dimensions.RegionIds, profile.RegionId);
        AppendOneHot(values, dimensions.OccupationIds, profile.OccupationId);
        AppendOneHot(values, dimensions.EducationLevelIds, profile.EducationLevelId);
        AppendOneHot(values, dimensions.LearningPurposeIds, profile.LearningPurposeId);

        return values.ToArray();
    }

    private static void AppendOneHot(List<double> values, IReadOnlyList<uint> activeIds, uint? selectedId)
    {
        foreach (var activeId in activeIds)
        {
            values.Add(selectedId == activeId ? 1.0 : 0.0);
        }
    }

    private static bool IsZeroVector(double[] vector)
    {
        return vector.All(value => value == 0.0);
    }
}
