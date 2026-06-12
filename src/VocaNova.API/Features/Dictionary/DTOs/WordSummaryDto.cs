using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordSummaryDto(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("image_url")] string? ImageUrl);
