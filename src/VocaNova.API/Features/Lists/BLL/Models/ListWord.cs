namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record ListWord(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    int CorrectCount,
    int WrongCount,
    string? Note,
    DateTime AddedAt);
