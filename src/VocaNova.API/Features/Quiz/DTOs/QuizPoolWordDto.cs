namespace VocaNova.API.Features.Quiz.DTOs;

public sealed record QuizPoolWordDto(
    uint WordId,
    DateTime AddedAt,
    int WrongCount = 0);
