using System.Text.Json.Serialization;

namespace VocaNova.API.Features.Quiz.Contracts.Responses;

public sealed record QuestionResponse(
    [property: JsonPropertyName("word_id")]
    uint WordId,
    [property: JsonPropertyName("sense_id")]
    uint SenseId,
    [property: JsonPropertyName("question_type")]
    int QuestionType,
    [property: JsonPropertyName("display_content")]
    string DisplayContent,
    [property: JsonPropertyName("expected_answer")]
    string ExpectedAnswer,
    [property: JsonPropertyName("choices")]
    IReadOnlyCollection<string> Choices);
