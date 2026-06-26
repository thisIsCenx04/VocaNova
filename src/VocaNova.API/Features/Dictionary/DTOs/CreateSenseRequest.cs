using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record CreateSenseRequest(
    [property: JsonPropertyName("sense_order")] int SenseOrder,
    [property: JsonPropertyName("word_class")] string? WordClass,
    [property: JsonPropertyName("english_definition")] string? EnglishDefinition,
    [property: JsonPropertyName("vietnamese_meaning")] string? VietnameseMeaning,
    [property: JsonPropertyName("examples")] IReadOnlyList<SenseExampleInput>? Examples = null);
