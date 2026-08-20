using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed record CreateRegionRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId);

public sealed record UpdateRegionRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parent_id")] uint? ParentId);
