namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record PersonalTopic(
    uint TopicId,
    uint? ListId,
    string Name,
    string? NameVi,
    string? Icon,
    int WordCount,
    bool ContainsWord);
