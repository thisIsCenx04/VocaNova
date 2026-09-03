using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record WordSummaryResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("image_url")] string? ImageUrl);
