using System;
using System.Collections.Generic;

namespace VocaNova.API.Infrastructure.Persistence.Entities;

public partial class AuditLog
{
    public uint LogId { get; set; }

    public uint UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public uint? EntityId { get; set; }

    public string? PayloadBefore { get; set; }

    public string? PayloadAfter { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

