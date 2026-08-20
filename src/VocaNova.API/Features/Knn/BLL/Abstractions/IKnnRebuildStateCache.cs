namespace VocaNova.API.Features.Knn.BLL.Abstractions;

public interface IKnnRebuildStateCache
{
    Task<DateTime?> GetLastRebuildAtAsync(CancellationToken cancellationToken = default);

    Task SetLastRebuildAtAsync(DateTime rebuiltAt, CancellationToken cancellationToken = default);
}
