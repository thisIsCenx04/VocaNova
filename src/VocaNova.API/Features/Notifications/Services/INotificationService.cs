using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.DTOs;

namespace VocaNova.API.Features.Notifications.Services;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationDto>>> ListAsync(uint userId, NotificationListQuery query, CancellationToken cancellationToken = default);
}
