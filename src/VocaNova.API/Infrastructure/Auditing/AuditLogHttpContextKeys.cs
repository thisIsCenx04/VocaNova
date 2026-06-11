namespace VocaNova.API.Infrastructure.Auditing;

public static class AuditLogHttpContextKeys
{
    public const string EntityType = "VocaNova.AuditLog.EntityType";
    public const string EntityId = "VocaNova.AuditLog.EntityId";
    public const string PayloadBefore = "VocaNova.AuditLog.PayloadBefore";
    public const string PayloadAfter = "VocaNova.AuditLog.PayloadAfter";
}
