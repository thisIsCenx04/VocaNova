using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed record CreateEducationLevelRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record UpdateEducationLevelRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("display_order")] int DisplayOrder);
