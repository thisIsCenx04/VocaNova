namespace VocaNova.API.Common.Abstractions.Transactions;

public interface IApplicationTransaction : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
