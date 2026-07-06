using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Notifications.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly VocaNovaDbContext _db;

    public NotificationRepository(VocaNovaDbContext db)
    {
        _db = db;
    }

    public async Task<int> AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0)
        {
            return 0;
        }

        await _db.Notifications.AddRangeAsync(notifications, cancellationToken);
        return await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<Notification>> ListByUserAsync(uint userId, int page, int limit, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId);

        return query.ToPagedResultAsync(page, limit, cancellationToken);
    }

    public Task<int> UnreadCountAsync(uint userId, CancellationToken cancellationToken = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<bool> MarkReadAsync(uint userId, uint notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllReadAsync(uint userId, CancellationToken cancellationToken = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task<IReadOnlyList<uint>> GetUserIdsAffectedByWordAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        // Active list entries (global query filter already excludes deleted rows) + any learning progress.
        var fromLists = _db.UserListWords
            .Where(w => w.WordId == wordId && w.Status == UserStatus.Active)
            .Select(w => w.UserId);

        var fromProgress = _db.UserWordProgresses
            .Where(p => p.WordId == wordId)
            .Select(p => p.UserId);

        return await fromLists.Union(fromProgress).Distinct().ToListAsync(cancellationToken);
    }

    public Task<string?> GetWordTextAsync(uint wordId, CancellationToken cancellationToken = default) =>
        _db.Words
            .IgnoreQueryFilters()
            .Where(w => w.WordId == wordId)
            .Select(w => (string?)w.Word1)
            .FirstOrDefaultAsync(cancellationToken);
}
