using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed record KnnLookupRequest(
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("limit")] int Limit = 20,
    [property: JsonPropertyName("q")] string? Q = null,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("include_deleted")] bool IncludeDeleted = false,
    [property: JsonPropertyName("sort_by")] string? SortBy = null,
    [property: JsonPropertyName("sort_direction")] string? SortDirection = null);
