using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record WordRecommendationResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("cefr_level")] string? CefrLevel,
    [property: JsonPropertyName("score")] double Score);
