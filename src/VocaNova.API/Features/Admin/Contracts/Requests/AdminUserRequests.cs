using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Admin.Contracts.Requests;

public sealed record AdminUserQueryRequest(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("search")] string? Search = null,
    [property: JsonPropertyName("includeDeleted")] bool IncludeDeleted = false,
    [property: JsonPropertyName("role")] string? Role = null,
    [property: JsonPropertyName("sortBy")] string? SortBy = null,
    [property: JsonPropertyName("sortDirection")] string? SortDirection = null);
