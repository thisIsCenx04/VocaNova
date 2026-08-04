using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record AnswerResultDto(
    [property: JsonPropertyName("session_id")] uint SessionId,
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("is_correct")] bool IsCorrect,
    [property: JsonPropertyName("expected_answer")] string ExpectedAnswer,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("ai_score")] float? AiScore,
    [property: JsonPropertyName("ai_explanation")] string? AiExplanation,
    [property: JsonPropertyName("ai_suggestion")] string? AiSuggestion,
    [property: JsonPropertyName("next_question")] QuestionDto? NextQuestion);
