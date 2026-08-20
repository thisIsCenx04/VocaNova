namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record UserListSummary(
    uint ListId,
    string ListName,
    int WordCount,
    DateTime CreatedAt);
