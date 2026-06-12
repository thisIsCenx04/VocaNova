using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

public sealed record WordDetailDto(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("word_key")] string WordKey,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic_uk")] string? PhoneticUk,
    [property: JsonPropertyName("phonetic_us")] string? PhoneticUs,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("is_phrase")] bool IsPhrase,
    [property: JsonPropertyName("senses")] IReadOnlyCollection<WordSenseDto> Senses,
    [property: JsonPropertyName("examples")] IReadOnlyCollection<WordExampleDto> Examples,
    [property: JsonPropertyName("relations")] IReadOnlyCollection<WordRelationDto> Relations,
    [property: JsonPropertyName("audio")] IReadOnlyCollection<WordAudioDto> Audio,
    [property: JsonPropertyName("derived_forms")] IReadOnlyCollection<WordDerivedFormDto> DerivedForms,
    [property: JsonPropertyName("idioms")] IReadOnlyCollection<WordIdiomDto> Idioms,
    [property: JsonPropertyName("topics")] IReadOnlyCollection<WordTopicDto> Topics);
