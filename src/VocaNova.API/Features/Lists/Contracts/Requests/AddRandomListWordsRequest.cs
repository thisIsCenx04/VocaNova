using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Lists.Contracts.Requests;

public sealed record AddRandomListWordsRequest(
    [property: JsonPropertyName("topic_id")] uint? TopicId,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("method")] string? Method);
