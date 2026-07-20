using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.DTOs;

public sealed record PersonalTopicDto(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("list_id")] uint? ListId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount,
    [property: JsonPropertyName("contains_word")] bool ContainsWord);
