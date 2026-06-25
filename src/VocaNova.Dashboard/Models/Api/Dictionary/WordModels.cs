using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Dictionary;

// Mirror các DTO dictionary của VocaNova.API (snake_case khớp [JsonPropertyName] phía API).

public sealed record WordListItem(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("topics")] IReadOnlyList<WordTopic> Topics);

public sealed record WordTopic(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon);

public sealed record TopicSummary(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount);
