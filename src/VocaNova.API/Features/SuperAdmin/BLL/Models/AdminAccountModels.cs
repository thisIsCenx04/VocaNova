namespace VocaNova.API.Features.SuperAdmin.BLL.Models;

public sealed record AdminAccountQuery(
    int Page = 1,
    int Limit = 20,
    string? Status = null,
    string? Search = null,
    bool IncludeDeleted = false,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record CreateAdminAccountModel(
    string? FullName,
    string? Email,
    string? Phone,
    string? Password,
    string? Status = null);

public sealed record UpdateAdminAccountModel(
    string? FullName,
    string? Email,
    string? Phone,
    string? Password = null,
    string? Status = null);

public sealed record AdminAccountModel(
    uint AdminId,
    string FullName,
    string? Email,
    string? Phone,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt);
