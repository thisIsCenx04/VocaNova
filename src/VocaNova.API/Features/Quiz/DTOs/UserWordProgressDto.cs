using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record UserWordProgressDto(
    [property: JsonPropertyName("progress_id")] uint ProgressId,
    [property: JsonPropertyName("user_id")] uint UserId,
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("test_count")] int TestCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("consecutive_correct")] int ConsecutiveCorrect,
    [property: JsonPropertyName("is_in_wrong_list")] bool IsInWrongList,
    [property: JsonPropertyName("mastery_level")] int MasteryLevel,
    [property: JsonPropertyName("srs_interval")] int SrsInterval,
    [property: JsonPropertyName("ease_factor")] float EaseFactor,
    [property: JsonPropertyName("last_tested_at")] DateTime? LastTestedAt,
    [property: JsonPropertyName("last_wrong_at")] DateTime? LastWrongAt,
    [property: JsonPropertyName("next_review_at")] DateTime? NextReviewAt,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);
