using VocaNova.API.Common.Results;
using VocaNova.API.Features.Notifications.DTOs;

namespace VocaNova.API.Features.Notifications.Services;

public interface INotificationService
{
    // Create a "word deleted" notification for every user that references the word. Returns number of notifications created.
    Task<int> NotifyWordDeletedAsync(uint wordId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<NotificationDto>>> ListAsync(uint userId, NotificationListQuery query, CancellationToken cancellationToken = default);

    Task<Result<int>> UnreadCountAsync(uint userId, CancellationToken cancellationToken = default);

    Task<Result<bool>> MarkReadAsync(uint userId, uint notificationId, CancellationToken cancellationToken = default);

    Task<Result<int>> MarkAllReadAsync(uint userId, CancellationToken cancellationToken = default);
}
