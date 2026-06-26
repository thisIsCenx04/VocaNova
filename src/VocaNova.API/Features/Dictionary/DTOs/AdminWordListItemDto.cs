using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.DTOs;

/// <summary>Một dòng trong bảng quản lý từ vựng dashboard (F057).</summary>
public sealed record AdminWordListItemDto(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("topics")] IReadOnlyCollection<WordTopicDto> Topics,
    [property: JsonPropertyName("word_type")] string? WordType);
