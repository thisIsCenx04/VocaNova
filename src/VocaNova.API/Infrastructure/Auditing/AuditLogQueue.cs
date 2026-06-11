using System.Threading.Channels;

namespace VocaNova.API.Infrastructure.Auditing;

public sealed class AuditLogQueue : IAuditLogQueue
{
    private readonly Channel<AuditLogMessage> _channel = Channel.CreateUnbounded<AuditLogMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public bool TryEnqueue(AuditLogMessage message)
    {
        return _channel.Writer.TryWrite(message);
    }

    public IAsyncEnumerable<AuditLogMessage> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
