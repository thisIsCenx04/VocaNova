namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record ListWordState(
    uint UserId,
    uint ListId,
    uint WordId,
    string Status);

public sealed record AddRandomListWordsResult(
    int AddedCount,
    IReadOnlyCollection<ListWord> Words);
