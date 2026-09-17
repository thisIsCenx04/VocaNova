namespace VocaNova.API.Features.Notifications.BLL.Models;

public sealed record MasteredWrongWordReference(
    uint ProgressId,
    uint WordId,
    string? WordText,
    DateTime MasteredAt);
