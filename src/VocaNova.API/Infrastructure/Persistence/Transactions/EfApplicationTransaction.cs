using Microsoft.EntityFrameworkCore.Storage;
using VocaNova.API.Common.Abstractions.Transactions;

namespace VocaNova.API.Infrastructure.Persistence.Transactions;

public sealed class EfApplicationTransaction : IApplicationTransaction
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly IDbContextTransaction _transaction;
    private bool _committed;
    private bool _rolledBack;

    public EfApplicationTransaction(VocaNovaDbContext dbContext, IDbContextTransaction transaction)
    {
        _dbContext = dbContext;
        _transaction = transaction;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_committed || _rolledBack)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        _rolledBack = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed && !_rolledBack)
        {
            await _transaction.RollbackAsync();
            _rolledBack = true;
        }

        await _transaction.DisposeAsync();
    }
}
