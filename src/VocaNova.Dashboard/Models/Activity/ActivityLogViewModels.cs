using VocaNova.Dashboard.Models.Api.Audit;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.Activity;

public sealed class ActivityLogQuery
{
    /// <summary>Lọc theo user_id (khớp param API `user_id`).</summary>
    public uint? UserId { get; set; }

    /// <summary>Lọc theo loại thực thể (khớp param API `entity`).</summary>
    public string? Entity { get; set; }

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 20;
}

public sealed class ActivityLogViewModel
{
    public ActivityLogQuery Query { get; init; } = new();

    public IReadOnlyList<AuditLogDto> Logs { get; init; } = Array.Empty<AuditLogDto>();

    public bool Loaded { get; init; }

    public PaginationInfo? Pagination { get; init; }
}
