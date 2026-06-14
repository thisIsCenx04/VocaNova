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
}
