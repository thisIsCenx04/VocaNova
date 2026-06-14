using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Services;

public interface IKnnLearningService
{
    Task<Result<double[]>> ComputeTopicAccuracyVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnNeighborDto>> FindKNearestAsync(
        uint userId,
        double[] vector,
        int k,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<WordRecommendationItem>>> GenerateWordRecommendationsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<WordRecommendationDto>>> GetWordRecommendationsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);
}
