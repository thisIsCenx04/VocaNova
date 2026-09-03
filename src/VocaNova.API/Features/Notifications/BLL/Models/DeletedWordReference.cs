namespace VocaNova.API.Features.Notifications.BLL.Models;

public sealed record DeletedWordReference(
    uint WordId,
    string? WordText,
    DateTime DeletedAt);
