using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Notifications.DTOs;

public sealed class NotificationListQuery
{
    public int Page { get; set; } = 1;

    public int Limit { get; set; } = AppSettings.DefaultPageLimit;
}
