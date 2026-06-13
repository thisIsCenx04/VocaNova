namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record QuestionDto(
    uint WordId,
    uint SenseId,
    int QuestionType,
    string DisplayContent,
    string ExpectedAnswer,
    IReadOnlyCollection<string> Choices);
