namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record WordSearchQuery(
    string? Query,
    int Page,
    int Limit,
    string? Cefr,
    uint? TopicId,
    bool? IsPhrase);

public sealed record TopicWordsQuery(int Page, int Limit);
