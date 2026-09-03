namespace VocaNova.API.Common.Abstractions.Transactions;

public interface IApplicationTransactionManager
{
    Task<IApplicationTransaction> BeginAsync(CancellationToken cancellationToken = default);
}
