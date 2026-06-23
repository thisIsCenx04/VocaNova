namespace VocaNova.Dashboard.Models.Api.Audit;

// Map từ `GET /api/admin/audit-logs` qua ApiJson.Default (SnakeCaseLower) — không field nào chứa số.

public sealed class AuditLogDto
{
    public uint LogId { get; set; }

    public uint UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public uint? EntityId { get; set; }

    public string? PayloadBefore { get; set; }

    public string? PayloadAfter { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
