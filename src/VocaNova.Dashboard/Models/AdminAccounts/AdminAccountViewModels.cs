using VocaNova.Dashboard.Models.Api.AdminAccounts;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Dashboard.Models.AdminAccounts;

public sealed class AdminAccountListQuery
{
    public string? Q { get; set; }

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 20;
}

public sealed class AdminAccountListViewModel
{
    public AdminAccountListQuery Query { get; init; } = new();

    public IReadOnlyList<AdminAccountDto> Accounts { get; init; } = Array.Empty<AdminAccountDto>();

    public bool Loaded { get; init; }

    public PaginationInfo? Pagination { get; init; }

    /// <summary>
    /// G9: false khi `GET /api/admin/admin-accounts` chưa deploy (404) → ẩn/disable hành động,
    /// hiện _StateBlock "Sắp có". Tự bật khi An deploy endpoint.
    /// </summary>
    public bool ApiAvailable { get; init; }

    public static readonly IReadOnlyList<string> RoleOptions = new[] { "admin", "super_admin" };
}

public sealed class AdminAccountFormViewModel
{
    public uint? Id { get; set; }

    public string? Phone { get; set; }

    public string? DisplayName { get; set; }

    public string? Password { get; set; }

    public string Role { get; set; } = "admin";

    public string? Error { get; set; }

    public bool IsEdit => Id.HasValue;
}
