using VocaNova.API.Features.Knn.BLL.Models;

namespace VocaNova.API.Features.Knn.BLL.Services;

public interface IKnnRebuildService
{
    bool IsRunning { get; }

    Task<KnnRebuildStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    void TriggerRebuild();

    Task RebuildAllAsync(CancellationToken cancellationToken = default);
}
