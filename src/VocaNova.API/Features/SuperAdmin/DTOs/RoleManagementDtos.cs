using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.DTOs;

public sealed record RoleQuery(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("type")] string? Type = null);

public sealed record SaveRoleRequest(
    [property: JsonPropertyName("role_name")] string? RoleName);

public sealed record RoleDto(
    [property: JsonPropertyName("role_id")] uint RoleId,
    [property: JsonPropertyName("role_name")] string RoleName);

public sealed record RoleUserDto(
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("status")] string Status);
