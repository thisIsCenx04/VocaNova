using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Knn.BLL.Models;
using VocaNova.API.Features.Knn.BLL.Abstractions;
using VocaNova.API.Features.Knn.BLL.Abstractions;

namespace VocaNova.API.Features.Knn.BLL.Services;

public sealed class KnnLearningService : IKnnLearningService
{
    private readonly IKnnLearningRepository _knnLearningRepository;
    private readonly IKnnProfileRepository? _knnProfileRepository;
    private readonly IKnnRuntimeConfigurationService? _runtimeConfig;
    private readonly IKnnWordRecommendationCache? _wordRecommendationCache;
    private readonly KnnOptions _options;
    private readonly ILogger<KnnLearningService>? _logger;

    public KnnLearningService(
        IKnnLearningRepository knnLearningRepository,
        IOptions<KnnOptions> options,
        IKnnWordRecommendationCache? wordRecommendationCache = null,
        ILogger<KnnLearningService>? logger = null,
        IKnnProfileRepository? knnProfileRepository = null,
        IKnnRuntimeConfigurationService? runtimeConfig = null)
    {
        _knnLearningRepository = knnLearningRepository;
        _wordRecommendationCache = wordRecommendationCache;
        _options = options.Value;
        _logger = logger;
        _knnProfileRepository = knnProfileRepository;
        _runtimeConfig = runtimeConfig;
    }

    public async Task<KnnOperationResult<double[]>> ComputeTopicAccuracyVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<double[]>.Unauthorized("Unauthorized.");
        }

        var sessionCount = await _knnLearningRepository.GetSessionCountAsync(userId, cancellationToken);
        if (sessionCount < _options.Learning.MinSessions)
        {
            return KnnOperationResult<double[]>.ValidationFailure("Not enough sessions for KNN learning recommendation.");
        }

        var topicIds = await _knnLearningRepository.GetActiveTopicIdsAsync(cancellationToken);
        var stats = await _knnLearningRepository.GetTopicAnswerStatsAsync(userId, cancellationToken);
        var statsByTopicId = stats.ToDictionary(stat => stat.TopicId);

        var vector = topicIds
            .Select(topicId =>
            {
                if (!statsByTopicId.TryGetValue(topicId, out var stat) || stat.TotalCount == 0)
                {
                    return 0.0;
                }

                return (double)stat.CorrectCount / stat.TotalCount;
            })
            .ToArray();

        return KnnOperationResult<double[]>.Success(vector);
    }

    public async Task<IReadOnlyCollection<KnnNeighbor>> FindKNearestAsync(
        uint userId,
        double[] vector,
        int k,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0 || vector.Length == 0 || k <= 0)
        {
            return Array.Empty<KnnNeighbor>();
        }

        var candidateUserIds = await _knnLearningRepository.GetEligibleUserIdsAsync(
            _options.Learning.MinSessions,
            userId,
            cancellationToken);
        var neighbors = new List<KnnNeighbor>();
        foreach (var candidateUserId in candidateUserIds)
        {
            var candidateVectorResult = await ComputeTopicAccuracyVectorAsync(candidateUserId, cancellationToken);
            if (!candidateVectorResult.IsSuccess || candidateVectorResult.Value is null)
            {
                continue;
            }

            var similarity = KnnMath.CosineSimilarity(vector, candidateVectorResult.Value);
            if (similarity >= _options.Learning.MinSimilarity)
            {
                neighbors.Add(new KnnNeighbor(candidateUserId, similarity));
            }
        }

        return neighbors
            .OrderByDescending(neighbor => neighbor.Similarity)
            .ThenBy(neighbor => neighbor.UserId)
            .Take(k)
            .ToArray();
    }

    public async Task<KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>> GenerateWordRecommendationsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var vectorResult = await ComputeTopicAccuracyVectorAsync(userId, cancellationToken);
        if (!vectorResult.IsSuccess || vectorResult.Value is null)
        {
            // Not enough quiz history to measure accuracy yet — fall back to the profile vector
            // so a newly registered user still gets a starting word list.
            _logger?.LogInformation(
                "Falling back to profile-based KNN word recommendation for user {UserId}: {Reason}",
                userId,
                vectorResult.Error);
            return await GenerateColdStartWordRecommendationsAsync(
                userId,
                vectorResult.Error,
                cancellationToken);
        }

        var neighbors = await FindKNearestAsync(
            userId,
            vectorResult.Value,
            _options.Learning.KValue,
            cancellationToken);
        if (neighbors.Count == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(Array.Empty<WordRecommendationItem>());
        }

        var similarityByUserId = neighbors.ToDictionary(
            neighbor => neighbor.UserId,
            neighbor => neighbor.Similarity);
        var masteredWords = await _knnLearningRepository.GetMasteredWordsAsync(
            similarityByUserId.Keys.ToArray(),
            _options.Learning.MinNeighborMasteryLevel,
            cancellationToken);
        var currentUserWordIds = await _knnLearningRepository.GetActiveListWordIdsAsync(
            userId,
            cancellationToken);
        var currentUserWordIdSet = currentUserWordIds.ToHashSet();

        var scoresByWordId = masteredWords
            .Where(word => !currentUserWordIdSet.Contains(word.WordId))
            .GroupBy(word => word.WordId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(word => similarityByUserId[word.UserId]));
        if (scoresByWordId.Count == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(Array.Empty<WordRecommendationItem>());
        }

        var recommendations = await _knnLearningRepository.GetWordRecommendationItemsAsync(
            scoresByWordId,
            _options.Learning.RecommendationCount,
            cancellationToken);

        await CacheRecommendationsAsync(userId, recommendations, cancellationToken);

        return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(recommendations);
    }

    /// <summary>
    /// Cold-start path: neighbours are found on the hybrid profile vector (sign-up demographics
    /// plus onboarding answers) instead of quiz accuracy, and the recommended words are the ones
    /// those neighbours are actually studying, narrowed to the topics the user cares about.
    /// </summary>
    private async Task<KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>>
        GenerateColdStartWordRecommendationsAsync(
            uint userId,
            string? accuracyVectorError,
            CancellationToken cancellationToken)
    {
        if (_knnProfileRepository is null)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.ValidationFailure(
                accuracyVectorError ?? "Unable to compute KNN learning vector.");
        }

        var profile = await _knnProfileRepository.GetProfileVectorSourceAsync(userId, cancellationToken);
        if (profile is null)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.ValidationFailure(
                "No learning profile available for cold-start recommendation.");
        }

        var dimensions = await _knnProfileRepository.GetActiveLookupDimensionsAsync(cancellationToken);
        var weights = _runtimeConfig is null
            ? _options.Vector
            : await _runtimeConfig.GetVectorOptionsAsync(cancellationToken);
        var userVector = KnnProfileVectorBuilder.Build(profile, dimensions, weights);
        if (KnnProfileVectorBuilder.IsZeroVector(userVector))
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.ValidationFailure(
                "Learning profile is empty; cannot build a cold-start vector.");
        }

        var candidateProfiles = await _knnProfileRepository.GetCandidateProfileSourcesAsync(
            userId,
            cancellationToken);
        var neighbors = candidateProfiles
            .Select(candidate => new KnnNeighbor(
                candidate.UserId,
                KnnMath.CosineSimilarity(
                    userVector,
                    KnnProfileVectorBuilder.Build(candidate, dimensions, weights))))
            .Where(neighbor => neighbor.Similarity >= _options.Learning.MinSimilarity)
            .OrderByDescending(neighbor => neighbor.Similarity)
            .ThenBy(neighbor => neighbor.UserId)
            .Take(_options.Learning.KValue)
            .ToArray();
        if (neighbors.Length == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(
                Array.Empty<WordRecommendationItem>());
        }

        var similarityByUserId = neighbors.ToDictionary(
            neighbor => neighbor.UserId,
            neighbor => neighbor.Similarity);
        var interestTopicIds = await _knnProfileRepository.GetActiveTopicIdsAsync(userId, cancellationToken);
        var neighborWords = await _knnLearningRepository.GetNeighborStudiedWordsAsync(
            similarityByUserId.Keys.ToArray(),
            interestTopicIds,
            cancellationToken);
        var currentUserWordIds = await _knnLearningRepository.GetActiveListWordIdsAsync(
            userId,
            cancellationToken);
        var currentUserWordIdSet = currentUserWordIds.ToHashSet();

        var scoresByWordId = neighborWords
            .Where(word => !currentUserWordIdSet.Contains(word.WordId))
            .GroupBy(word => word.WordId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(word => similarityByUserId[word.UserId]));
        if (scoresByWordId.Count == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(
                Array.Empty<WordRecommendationItem>());
        }

        var recommendations = await _knnLearningRepository.GetWordRecommendationItemsAsync(
            scoresByWordId,
            _options.Learning.ColdStartRecommendationCount,
            cancellationToken);

        await CacheRecommendationsAsync(userId, recommendations, cancellationToken);

        return KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>.Success(recommendations);
    }

    private async Task CacheRecommendationsAsync(
        uint userId,
        IReadOnlyCollection<WordRecommendationItem> recommendations,
        CancellationToken cancellationToken)
    {
        if (_wordRecommendationCache is null)
        {
            return;
        }

        await _wordRecommendationCache.SetAsync(
            userId,
            recommendations,
            TimeSpan.FromMinutes(_options.Learning.CacheTtlMinutes),
            cancellationToken);
    }

    public async Task<KnnOperationResult<IReadOnlyCollection<WordRecommendation>>> GetWordRecommendationsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendation>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = NormalizeLimit(limit);
        if (normalizedLimit is null)
        {
            return KnnOperationResult<IReadOnlyCollection<WordRecommendation>>.ValidationFailure(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cachedRecommendations = _wordRecommendationCache is null
            ? null
            : await _wordRecommendationCache.GetAsync(userId, cancellationToken);
        if (cachedRecommendations is null)
        {
            // Nothing precomputed yet — a brand-new user would otherwise have to wait for the
            // next rebuild pass before seeing any words, so build them on demand.
            var generated = await GenerateWordRecommendationsAsync(userId, cancellationToken);
            if (!generated.IsSuccess || generated.Value is null || generated.Value.Count == 0)
            {
                return KnnOperationResult<IReadOnlyCollection<WordRecommendation>>.Success(
                    Array.Empty<WordRecommendation>());
            }

            cachedRecommendations = generated.Value;
        }

        var scoresByWordId = cachedRecommendations
            .GroupBy(word => word.WordId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(word => word.Score));
        var recommendations = await _knnLearningRepository.GetWordRecommendationsAsync(
            scoresByWordId,
            normalizedLimit.Value,
            cancellationToken);

        return KnnOperationResult<IReadOnlyCollection<WordRecommendation>>.Success(recommendations);
    }

    private int? NormalizeLimit(int? limit)
    {
        var normalized = limit ?? 10;
        return normalized <= 0 || normalized > AppSettings.MaxPageLimit
            ? null
            : normalized;
    }
}
