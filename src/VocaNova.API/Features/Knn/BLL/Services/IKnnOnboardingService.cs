using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Services;

public interface IKnnOnboardingService
{
    Task<double[]> ComputeProfileVectorAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    double CosineSimilarity(double[] a, double[] b);

    Task<KnnOperationResult<LearningProfileOptions>> GetLearningProfileOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<IReadOnlyCollection<TopicRecommendation>>> RecommendTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<IReadOnlyCollection<PersonalTopicRecommendation>>> RecommendPersonalTopicsAsync(
        uint userId,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<bool>> AcceptTopicAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default);

    Task<KnnOperationResult<int>> SelectTopicsAsync(
        uint userId,
        IReadOnlyCollection<uint> topicIds,
        CancellationToken cancellationToken = default);
}
