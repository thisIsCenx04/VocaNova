using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record CreateSessionResponse(
    [property: JsonPropertyName("session")] QuizSessionDto Session,
    [property: JsonPropertyName("first_question")] QuestionDto FirstQuestion);
