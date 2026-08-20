using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;

namespace VocaNova.API.Features.Notifications.BLL.Models;

public sealed class NotificationListResult
{
    private NotificationListResult(
        PagedCollection<Notification>? value,
        string? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public PagedCollection<Notification>? Value { get; }

    public string? Error { get; }

    public static NotificationListResult Success(PagedCollection<Notification> value) =>
        new(value, null);

    public static NotificationListResult ValidationFailure(string error) =>
        new(null, error);
}
