namespace VocaNova.API.Infrastructure.Caching;

public interface IKnnRebuildStateCache
{
    Task<DateTime?> GetLastRebuildAtAsync(CancellationToken cancellationToken = default);

    Task SetLastRebuildAtAsync(DateTime rebuiltAt, CancellationToken cancellationToken = default);
}
