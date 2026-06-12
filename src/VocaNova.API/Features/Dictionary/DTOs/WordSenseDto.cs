using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordSenseDto(
    [property: JsonPropertyName("sense_id")] uint SenseId,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("word_class")] string WordClass,
    [property: JsonPropertyName("english_definition")] string EnglishDefinition,
    [property: JsonPropertyName("vietnamese_meaning")] string? VietnameseMeaning,
    [property: JsonPropertyName("examples")] IReadOnlyCollection<WordExampleDto> Examples,
    [property: JsonPropertyName("relations")] IReadOnlyCollection<WordRelationDto> Relations);
