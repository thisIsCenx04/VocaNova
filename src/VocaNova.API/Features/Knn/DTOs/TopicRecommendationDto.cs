using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.DTOs;

public sealed record TopicRecommendationDto(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("topic_name")] string TopicName,
    [property: JsonPropertyName("topic_name_vi")] string? TopicNameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount,
    [property: JsonPropertyName("recommendation_score")] double RecommendationScore);
