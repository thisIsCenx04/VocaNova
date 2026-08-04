using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record CreateWordRequest(
    [property: JsonPropertyName("word")] string? Word,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("phonetic_us")] string? PhoneticUs,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("is_phrase")] bool IsPhrase = false);
