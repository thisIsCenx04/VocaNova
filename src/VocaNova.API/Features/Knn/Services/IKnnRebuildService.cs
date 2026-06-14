using VocaNova.API.Features.Knn.DTOs;

namespace VocaNova.API.Features.Knn.Services;

public interface IKnnRebuildService
{
    bool IsRunning { get; }

    Task<KnnRebuildStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    void TriggerRebuild();

    Task RebuildAllAsync(CancellationToken cancellationToken = default);
}
