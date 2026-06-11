namespace VocaNova.API.Infrastructure.Auditing;

public interface IAuditLogQueue
{
    bool TryEnqueue(AuditLogMessage message);

    IAsyncEnumerable<AuditLogMessage> DequeueAllAsync(CancellationToken cancellationToken);
}
