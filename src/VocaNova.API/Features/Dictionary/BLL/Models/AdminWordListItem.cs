namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record AdminWordListItem(
    uint WordId,
    string Word,
    string? Cefr,
    string? Phonetic,
    string Status,
    string? ImageUrl,
    string? PrimaryMeaning,
    IReadOnlyCollection<WordTopic> Topics,
    string? WordType);
