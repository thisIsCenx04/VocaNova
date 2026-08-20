using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Requests;

public sealed record SubmitAnswerRequest(
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("user_answer")] string? UserAnswer,
    [property: JsonPropertyName("list_id")] uint? ListId = null);
