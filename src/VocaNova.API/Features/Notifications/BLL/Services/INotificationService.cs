using VocaNova.API.Features.Notifications.BLL.Models;

namespace VocaNova.API.Features.Notifications.BLL.Services;

public interface INotificationService
{
    Task<NotificationListResult> ListAsync(
        uint userId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default);
}
