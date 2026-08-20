using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Knn.Contracts.Responses;

public sealed record PersonalTopicRecommendationWordResponse(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic")] string? Phonetic,
    [property: JsonPropertyName("cefr")] string? Cefr,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning);

public sealed record PersonalTopicRecommendationResponse(
    [property: JsonPropertyName("topic_id")] uint TopicId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_vi")] string? NameVi,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("word_count")] int WordCount,
    [property: JsonPropertyName("recommendation_score")] double RecommendationScore,
    [property: JsonPropertyName("words")] IReadOnlyCollection<PersonalTopicRecommendationWordResponse> Words);
