namespace VocaNova.API.Features.SuperAdmin.BLL.Models;

public sealed record RoleQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    string? Type = null,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record SaveRoleModel(
    string? RoleName);

public sealed record RoleModel(
    uint RoleId,
    string RoleName);

public sealed record RoleUserModel(
    uint UserId,
    string DisplayName,
    string? Email,
    string? Phone,
    string Status);
