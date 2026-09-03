using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Responses;

public sealed record QuizResultResponse(
    [property: JsonPropertyName("session_id")] uint SessionId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("correct_count")] int CorrectCount,
    [property: JsonPropertyName("wrong_count")] int WrongCount,
    [property: JsonPropertyName("question_count")] int QuestionCount,
    [property: JsonPropertyName("answered_count")] int AnsweredCount,
    [property: JsonPropertyName("accuracy")] float Accuracy,
    [property: JsonPropertyName("duration_sec")] int? DurationSec,
    [property: JsonPropertyName("max_streak")] int MaxStreak,
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("ended_at")] DateTime? EndedAt,
    [property: JsonPropertyName("answers")] IReadOnlyCollection<TestAnswerResponse> Answers);
