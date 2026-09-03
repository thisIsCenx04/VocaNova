using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Dictionary.Contracts.Requests;

public sealed record UpdateTopicRequest(
    [property: JsonPropertyName("topic_name")] string? TopicName,
    [property: JsonPropertyName("topic_name_vi")] string? TopicNameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_ids")] IReadOnlyCollection<uint>? WordIds = null);
