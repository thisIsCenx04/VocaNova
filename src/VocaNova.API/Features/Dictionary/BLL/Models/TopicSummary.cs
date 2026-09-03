namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record TopicSummary(
    uint TopicId,
    string Name,
    string? NameVi,
    string? Icon,
    int WordCount);
