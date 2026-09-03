namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record AdminWordQuery(
    string? Q,
    string? Cefr,
    uint? TopicId,
    string? WordType,
    string? Status,
    bool IncludeDeleted,
    int Page,
    int Limit,
    string? SortBy,
    string? SortDirection);

public sealed record AdminTopicQuery(string? Q, string? Status, bool IncludeDeleted);
