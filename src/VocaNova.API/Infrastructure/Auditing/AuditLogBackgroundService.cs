using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Infrastructure.Auditing;

public sealed class AuditLogBackgroundService : BackgroundService
{
    private readonly IAuditLogQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogBackgroundService> _logger;

    public AuditLogBackgroundService(
        IAuditLogQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditLogBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<VocaNovaDbContext>();

                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = message.UserId,
                    Action = message.Action,
                    EntityType = message.EntityType,
                    EntityId = message.EntityId,
                    IpAddress = message.IpAddress,
                    PayloadBefore = message.PayloadBefore,
                    PayloadAfter = message.PayloadAfter,
                    CreatedAt = message.CreatedAt,
                });

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Failed to persist audit log for {Action} {EntityType}/{EntityId}.", message.Action, message.EntityType, message.EntityId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error while persisting audit log.");
            }
        }
    }
}
