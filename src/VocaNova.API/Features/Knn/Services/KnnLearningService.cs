using Microsoft.Extensions.Options;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;
using VocaNova.API.Features.Knn.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Knn.Services;

public sealed class KnnLearningService : IKnnLearningService
{
    private const int MinMasteryLevelForNeighborWords = 3;

    private readonly IKnnLearningRepository _knnLearningRepository;
    private readonly IKnnWordRecommendationCache? _wordRecommendationCache;
    private readonly KnnOptions _options;
    private readonly ILogger<KnnLearningService>? _logger;

    public KnnLearningService(
        IKnnLearningRepository knnLearningRepository,
        IOptions<KnnOptions> options,
        IKnnWordRecommendationCache? wordRecommendationCache = null,
        ILogger<KnnLearningService>? logger = null)
    {
        _knnLearningRepository = knnLearningRepository;
        _wordRecommendationCache = wordRecommendationCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<double[]>> ComputeTopicAccuracyVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<double[]>.Unauthorized("Unauthorized.");
        }

        var sessionCount = await _knnLearningRepository.GetSessionCountAsync(userId, cancellationToken);
        if (sessionCount < _options.Learning.MinSessions)
        {
            return Result<double[]>.Fail("Not enough sessions for KNN learning recommendation.");
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

        return Result<double[]>.Ok(vector);
    }

    public async Task<IReadOnlyCollection<KnnNeighborDto>> FindKNearestAsync(
        uint userId,
        double[] vector,
        int k,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0 || vector.Length == 0 || k <= 0)
        {
            return Array.Empty<KnnNeighborDto>();
        }

        var candidateUserIds = await _knnLearningRepository.GetEligibleUserIdsAsync(
            _options.Learning.MinSessions,
            userId,
            cancellationToken);
        var neighbors = new List<KnnNeighborDto>();
        foreach (var candidateUserId in candidateUserIds)
        {
            var candidateVectorResult = await ComputeTopicAccuracyVectorAsync(candidateUserId, cancellationToken);
            if (!candidateVectorResult.IsSuccess || candidateVectorResult.Value is null)
            {
                continue;
            }

            var similarity = KnnMathHelper.CosineSimilarity(vector, candidateVectorResult.Value);
            if (similarity >= _options.Learning.MinSimilarity)
            {
                neighbors.Add(new KnnNeighborDto(candidateUserId, similarity));
            }
        }

        return neighbors
            .OrderByDescending(neighbor => neighbor.Similarity)
            .ThenBy(neighbor => neighbor.UserId)
            .Take(k)
            .ToArray();
    }

    public async Task<Result<IReadOnlyCollection<WordRecommendationItem>>> GenerateWordRecommendationsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var vectorResult = await ComputeTopicAccuracyVectorAsync(userId, cancellationToken);
        if (!vectorResult.IsSuccess || vectorResult.Value is null)
        {
            _logger?.LogInformation(
                "Skipping KNN word recommendation for user {UserId}: {Reason}",
                userId,
                vectorResult.Error);
            return Result<IReadOnlyCollection<WordRecommendationItem>>.Fail(
                vectorResult.Error ?? "Unable to compute KNN learning vector.");
        }

        var neighbors = await FindKNearestAsync(
            userId,
            vectorResult.Value,
            _options.Learning.KValue,
            cancellationToken);
        if (neighbors.Count == 0)
        {
            return Result<IReadOnlyCollection<WordRecommendationItem>>.Ok(Array.Empty<WordRecommendationItem>());
        }

        var similarityByUserId = neighbors.ToDictionary(
            neighbor => neighbor.UserId,
            neighbor => neighbor.Similarity);
        var masteredWords = await _knnLearningRepository.GetMasteredWordsAsync(
            similarityByUserId.Keys.ToArray(),
            MinMasteryLevelForNeighborWords,
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
            return Result<IReadOnlyCollection<WordRecommendationItem>>.Ok(Array.Empty<WordRecommendationItem>());
        }

        var recommendations = await _knnLearningRepository.GetWordRecommendationItemsAsync(
            scoresByWordId,
            _options.Learning.RecommendationCount,
            cancellationToken);

        if (_wordRecommendationCache is not null)
        {
            await _wordRecommendationCache.SetAsync(
                userId,
                recommendations,
                TimeSpan.FromHours(_options.Learning.RebuildIntervalHours),
                cancellationToken);
        }

        return Result<IReadOnlyCollection<WordRecommendationItem>>.Ok(recommendations);
    }

    public async Task<Result<IReadOnlyCollection<WordRecommendationDto>>> GetWordRecommendationsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<WordRecommendationDto>>.Unauthorized("Unauthorized.");
        }

        var normalizedLimit = NormalizeLimit(limit);
        if (normalizedLimit is null)
        {
            return Result<IReadOnlyCollection<WordRecommendationDto>>.Fail(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cachedRecommendations = _wordRecommendationCache is null
            ? null
            : await _wordRecommendationCache.GetAsync(userId, cancellationToken);
        if (cachedRecommendations is null)
        {
            return Result<IReadOnlyCollection<WordRecommendationDto>>.Ok(Array.Empty<WordRecommendationDto>());
        }

        var scoresByWordId = cachedRecommendations
            .GroupBy(word => word.WordId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(word => word.Score));
        var recommendations = await _knnLearningRepository.GetWordRecommendationDtosAsync(
            scoresByWordId,
            normalizedLimit.Value,
            cancellationToken);

        return Result<IReadOnlyCollection<WordRecommendationDto>>.Ok(recommendations);
    }

    private int? NormalizeLimit(int? limit)
    {
        var normalized = limit ?? 10;
        return normalized <= 0 || normalized > AppSettings.MaxPageLimit
            ? null
            : normalized;
    }
}
