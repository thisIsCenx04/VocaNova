using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordDerivedFormDto(
    [property: JsonPropertyName("derived_id")] uint DerivedId,
    [property: JsonPropertyName("derived_word")] string DerivedWord,
    [property: JsonPropertyName("linked_word_id")] uint? LinkedWordId,
    [property: JsonPropertyName("word_class")] string? WordClass);
