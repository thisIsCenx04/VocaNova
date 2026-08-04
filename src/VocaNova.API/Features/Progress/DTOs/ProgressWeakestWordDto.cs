using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Progress.DTOs;

public sealed record ProgressWeakestWordDto(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("primary_meaning")] string? PrimaryMeaning,
    [property: JsonPropertyName("test_count")] int TestCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("accuracy_rate")] float AccuracyRate,
    [property: JsonPropertyName("mastery_level")] int MasteryLevel,
    [property: JsonPropertyName("last_wrong_at")] DateTime? LastWrongAt,
    [property: JsonPropertyName("next_review_at")] DateTime? NextReviewAt);
