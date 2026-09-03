using System.Text.Json.Serialization;

namespace VocaNova.API.Features.SuperAdmin.Contracts.Requests;

public sealed record AdminAccountQueryRequest(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("include_deleted")] bool IncludeDeleted = false,
    [property: JsonPropertyName("sort_by")] string? SortBy = null,
    [property: JsonPropertyName("sort_direction")] string? SortDirection = null);

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
