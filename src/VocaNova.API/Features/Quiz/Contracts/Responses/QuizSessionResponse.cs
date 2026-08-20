using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Responses;

public sealed record QuizSessionResponse(
    [property: JsonPropertyName("session_id")] uint SessionId,
    [property: JsonPropertyName("answer_method")] string AnswerMethod,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("question_type")] int QuestionType,
    [property: JsonPropertyName("scope_type")] string ScopeType,
    [property: JsonPropertyName("scope_date_from")] DateOnly? ScopeDateFrom,
    [property: JsonPropertyName("scope_date_to")] DateOnly? ScopeDateTo,
    [property: JsonPropertyName("word_order")] string WordOrder,
    [property: JsonPropertyName("word_limit")] int? WordLimit,
    [property: JsonPropertyName("time_limit_sec")] int? TimeLimitSec,
    [property: JsonPropertyName("lives")] int? Lives,
    [property: JsonPropertyName("question_count")] int QuestionCount,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("topic_ids")] IReadOnlyCollection<uint> TopicIds,
    [property: JsonPropertyName("list_id")] uint? ListId = null);
