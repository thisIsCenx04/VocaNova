using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordIdiomDto(
    [property: JsonPropertyName("idiom_id")] uint IdiomId,
    [property: JsonPropertyName("idiom_text")] string IdiomText,
    [property: JsonPropertyName("meaning_en")] string? MeaningEn,
    [property: JsonPropertyName("meaning_vi")] string? MeaningVi);
