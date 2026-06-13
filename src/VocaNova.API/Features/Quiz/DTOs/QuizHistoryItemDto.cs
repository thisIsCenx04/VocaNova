using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record QuizHistoryItemDto(
    [property: JsonPropertyName("session_id")] uint SessionId,
    [property: JsonPropertyName("answer_method")] string AnswerMethod,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("question_type")] int QuestionType,
    [property: JsonPropertyName("question_count")] int QuestionCount,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("accuracy")] float Accuracy,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("max_streak")] int MaxStreak,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("ended_at")] DateTime? EndedAt);
