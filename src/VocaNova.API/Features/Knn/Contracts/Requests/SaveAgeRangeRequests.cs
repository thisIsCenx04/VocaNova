using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Requests;

public sealed record CreateAgeRangeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder);

public sealed record UpdateAgeRangeRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("min_age")] int? MinAge,
    [property: JsonPropertyName("max_age")] int? MaxAge,
    [property: JsonPropertyName("display_order")] int DisplayOrder);
