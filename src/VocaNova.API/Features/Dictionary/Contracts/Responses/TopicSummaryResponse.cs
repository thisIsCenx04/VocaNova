using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Responses;

public sealed record TopicSummaryResponse(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount);
