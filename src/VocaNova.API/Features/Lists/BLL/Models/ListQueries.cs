namespace VocaNova.API.Features.Lists.BLL.Models;

public sealed record ListWordsQuery(int Page, int Limit);

public sealed record PersonalTopicQuery(uint? WordId);

public sealed record ListOwnership(uint ListId, uint UserId, string Status, string ListName);
