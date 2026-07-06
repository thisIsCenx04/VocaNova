using VocaNova.API.Common.Results;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Notifications.Repositories;

public interface INotificationRepository
{
    Task<int> AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> ListByUserAsync(uint userId, int page, int limit, bool unreadOnly, CancellationToken cancellationToken = default);

    Task<int> UnreadCountAsync(uint userId, CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(uint userId, uint notificationId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(uint userId, CancellationToken cancellationToken = default);

    // Distinct user_ids that reference a word (active list entries + learning progress). Used to notify on soft-delete.
    Task<IReadOnlyList<uint>> GetUserIdsAffectedByWordAsync(uint wordId, CancellationToken cancellationToken = default);

    Task<string?> GetWordTextAsync(uint wordId, CancellationToken cancellationToken = default);
}
