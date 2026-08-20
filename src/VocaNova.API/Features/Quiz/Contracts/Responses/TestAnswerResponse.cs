using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Responses;

public sealed record TestAnswerResponse(
    [property: JsonPropertyName("answer_id")] uint AnswerId,
    [property: JsonPropertyName("word_id")] uint WordId,
    [property: JsonPropertyName("sense_id")] uint? SenseId,
    [property: JsonPropertyName("question_number")] int QuestionNumber,
    [property: JsonPropertyName("question_type")] int QuestionType,
    [property: JsonPropertyName("display_content")] string DisplayContent,
    [property: JsonPropertyName("expected_answer")] string ExpectedAnswer,
    [property: JsonPropertyName("user_answer")] string? UserAnswer,
    [property: JsonPropertyName("is_correct")] bool? IsCorrect,
    [property: JsonPropertyName("ai_score")] float? AiScore,
    [property: JsonPropertyName("ai_explanation")] string? AiExplanation,
    [property: JsonPropertyName("ai_suggestion")] string? AiSuggestion);
