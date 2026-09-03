using VocaNova.API.Common.Abstractions.Transactions;

namespace VocaNova.API.Infrastructure.Persistence.Transactions;

public sealed class EfApplicationTransactionManager : IApplicationTransactionManager
{
    private readonly VocaNovaDbContext _dbContext;

    public EfApplicationTransactionManager(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IApplicationTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfApplicationTransaction(_dbContext, transaction);
    }
}
