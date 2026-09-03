namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record WordSummary(
    uint WordId,
    string Word,
    string? Phonetic,
    string? Cefr,
    string? PrimaryMeaning,
    string? ImageUrl);
