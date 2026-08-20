using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record AdminWordListItemResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("topics")] IReadOnlyCollection<WordTopicResponse> Topics,
    [property: JsonPropertyName("word_type")] string? WordType);
