using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Notifications.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Notifications.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Notifications.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Notifications.DAL.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public NotificationRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedCollection<DeletedWordReference>> ListDeletedWordsAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDeletedWordsForUserQuery(userId)
            .OrderByDescending(word => word.UpdatedAt)
            .ThenByDescending(word => word.WordId);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(NotificationPersistenceMappings.ToDeletedWordReference)
            .ToListAsync(cancellationToken);

        return new PagedCollection<DeletedWordReference>(items, page, limit, totalItems);
    }

    public async Task<PagedCollection<MasteredWrongWordReference>> ListMasteredWrongWordsAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserWordProgresses
            .AsNoTracking()
            .Where(progress =>
                progress.UserId == userId
                && !progress.IsInWrongList
                && progress.MasteryLevel >= 5
                && progress.LastWrongAt != null
                && progress.Word.Status == UserStatus.Active)
            .OrderByDescending(progress => progress.UpdatedAt)
            .ThenByDescending(progress => progress.ProgressId);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(NotificationPersistenceMappings.ToMasteredWrongWordReference)
            .ToListAsync(cancellationToken);

        return new PagedCollection<MasteredWrongWordReference>(items, page, limit, totalItems);
    }

    private IQueryable<Word> BuildDeletedWordsForUserQuery(uint userId) =>
        _dbContext.Words
            .IgnoreQueryFilters()
            .Where(word => word.Status == UserStatus.Deleted)
            .Where(word =>
                _dbContext.UserListWords.Any(listWord =>
                    listWord.WordId == word.WordId
                    && listWord.UserId == userId
                    && listWord.Status == UserStatus.Active)
                || _dbContext.UserWordProgresses.Any(progress =>
                    progress.WordId == word.WordId
                    && progress.UserId == userId));
}
