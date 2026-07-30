using System.ComponentModel.DataAnnotations;
using VocaNova.Dashboard.Models.Api.SuperAdmin;

namespace VocaNova.Dashboard.Models.Roles;

public sealed class RoleManagementViewModel
{
    public IReadOnlyList<ManagedRole> Roles { get; init; } = [];
    public bool RolesUnavailable { get; init; }
    public string? Search { get; init; }
    public string? Type { get; init; }
    public int TotalRoles { get; init; }

    /// <summary>Cột đang sort (id | name | type).</summary>
    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed class AdminUserAssignmentViewModel
{
    public AdminUserAssignmentOverview Assignments { get; init; } = new([], []);
    public IReadOnlyList<AssignmentUser> Users { get; init; } = [];
    public uint? SelectedAdminId { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
    public int TotalUsers { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalUsers / (double)PageSize));
}

public sealed class SaveRoleViewModel
{
    public uint? RoleId { get; set; }

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 30 characters.")]
    [RegularExpression("^[a-z][a-z0-9_]*$", ErrorMessage = "Role name may only contain lowercase letters, numbers, and underscores.")]
    public string? RoleName { get; set; }

}
