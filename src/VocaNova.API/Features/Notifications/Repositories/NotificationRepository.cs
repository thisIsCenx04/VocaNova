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

    public Task<PagedResult<DeletedWordRef>> ListWordDeletedAsync(uint userId, int page, int limit, CancellationToken cancellationToken = default) =>
        BuildDeletedWordsForUserQuery(userId)
            .OrderByDescending(w => w.UpdatedAt)
            .ThenByDescending(w => w.WordId)
            .Select(w => new DeletedWordRef(w.WordId, w.Word1, w.UpdatedAt))
            .ToPagedResultAsync(page, limit, cancellationToken);

    // Words that are soft-deleted from the dictionary but the user still references, either via an
    // active list entry or learning progress. IgnoreQueryFilters is required because the global
    // filter hides deleted words (and it also disables the user_list_words filter, so the
    // Active check is stated explicitly here).
    private IQueryable<Word> BuildDeletedWordsForUserQuery(uint userId) =>
        _db.Words
            .IgnoreQueryFilters()
            .Where(w => w.Status == UserStatus.Deleted)
            .Where(w =>
                _db.UserListWords.Any(l => l.WordId == w.WordId && l.UserId == userId && l.Status == UserStatus.Active)
                || _db.UserWordProgresses.Any(p => p.WordId == w.WordId && p.UserId == userId));
}
