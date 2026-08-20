using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Responses;

public sealed record CreateSessionResponse(
    [property: JsonPropertyName("session")] QuizSessionResponse Session,
    [property: JsonPropertyName("first_question")] QuestionResponse FirstQuestion);
