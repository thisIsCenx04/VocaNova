using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Notifications.Contracts.Requests;

public sealed class NotificationListRequest
{
    public int Page { get; set; } = 1;

    public int Limit { get; set; } = AppSettings.DefaultPageLimit;
}
