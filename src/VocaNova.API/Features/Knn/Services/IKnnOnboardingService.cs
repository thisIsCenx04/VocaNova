using VocaNova.API.Common.Results;
using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Services;

public interface IKnnOnboardingService
{
    Task<double[]> ComputeProfileVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    double CosineSimilarity(double[] a, double[] b);

    Task<Result<IReadOnlyCollection<TopicRecommendationDto>>> RecommendTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> AcceptTopicAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default);
}
