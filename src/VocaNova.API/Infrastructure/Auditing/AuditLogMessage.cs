namespace VocaNova.API.Infrastructure.Auditing;

public sealed record AuditLogMessage(
    uint UserId,
    string Action,
    string EntityType,
    uint? EntityId,
    string? IpAddress,
    string? PayloadBefore,
    string? PayloadAfter,
    DateTime CreatedAt);
