using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.Contracts.Requests;

public sealed record RoleQueryRequest(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("type")] string? Type = null,
    [property: JsonPropertyName("sort_by")] string? SortBy = null,
    [property: JsonPropertyName("sort_direction")] string? SortDirection = null);

public sealed record SaveRoleRequest(
    [property: JsonPropertyName("role_name")] string? RoleName);
