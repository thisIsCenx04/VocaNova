using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Services;

public interface IKnnLearningService
{
    Task<KnnOperationResult<double[]>> ComputeTopicAccuracyVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<KnnNeighbor>> FindKNearestAsync(
        uint userId,
        double[] vector,
        int k,
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<IReadOnlyCollection<WordRecommendationItem>>> GenerateWordRecommendationsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<IReadOnlyCollection<WordRecommendation>>> GetWordRecommendationsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);
}
