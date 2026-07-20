using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.DTOs;

public sealed record AdminAccountQuery(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("include_deleted")] bool IncludeDeleted = false);

public sealed record CreateAdminAccountRequest(
    [property: JsonPropertyName("full_name")] string? FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("status")] string? Status = null);

public sealed record UpdateAdminAccountRequest(
    [property: JsonPropertyName("full_name")] string? FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("password")] string? Password = null,
    [property: JsonPropertyName("status")] string? Status = null);

public sealed record AdminAccountDto(
    [property: JsonPropertyName("admin_id")] uint AdminId,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("last_login_at")] DateTime? LastLoginAt);
