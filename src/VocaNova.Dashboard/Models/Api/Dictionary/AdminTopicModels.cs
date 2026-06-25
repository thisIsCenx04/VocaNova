using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Models.Api.Dictionary;

// Mirror AdminTopicDto của VocaNova.API (GET /api/admin/topics, F061).
public sealed record AdminTopic(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("topic_name")] string TopicName,
    [property: JsonPropertyName("topic_name_vi")] string? TopicNameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("word_count")] int WordCount);
