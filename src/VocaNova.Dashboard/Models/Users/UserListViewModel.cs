using VocaNova.Dashboard.Models.Api.Users;

namespace VocaNova.Dashboard.Models.Users;

public sealed class UserListViewModel
{
    public IReadOnlyList<AdminUserSummary> Items { get; init; } = Array.Empty<AdminUserSummary>();

    public string? Status { get; init; }

    public string? Search { get; init; }

    public string? Role { get; init; }

    public bool IncludeDeleted { get; init; }

    public int Page { get; init; } = 1;

    public int Limit { get; init; } = 10;

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    /// <summary>Cột đang sort (id | name | email | status | phone).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static readonly IReadOnlyList<string> Statuses = new[] { "active", "locked", "deleted" };

    public static readonly IReadOnlyList<(string Value, string Label)> Roles = new[]
    {
        ("super_admin", "Super admin"),
        ("admin", "Admin"),
        ("user", "User"),
        ("guest", "Guest"),
    };
}
