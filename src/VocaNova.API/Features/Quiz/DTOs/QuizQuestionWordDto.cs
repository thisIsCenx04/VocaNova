namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record QuizQuestionWordDto(
    uint WordId,
    string Word,
    uint SenseId,
    string WordClass,
    string EnglishDefinition,
    string? VietnameseMeaning,
    IReadOnlyCollection<uint> TopicIds);
