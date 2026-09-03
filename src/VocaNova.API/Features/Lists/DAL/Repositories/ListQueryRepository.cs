using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Lists.DAL.Repositories;

public sealed class ListQueryRepository : IListQueryRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public ListQueryRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserListSummary>> GetListsAsync(
        uint userId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => list.UserId == userId
                && list.Status == UserStatus.Active
                && !list.ListName.StartsWith(PersonalTopicListName.Prefix))
            .OrderByDescending(list => list.CreatedAt)
            .ThenByDescending(list => list.ListId)
            .Select(ListPersistenceMappings.ToUserListSummary)
            .ToListAsync(cancellationToken);

    public async Task<ListLookupResult<PagedCollection<ListWord>>> GetOwnedListWordsAsync(
        uint userId,
        uint listId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (listId == 0)
        {
            return ListLookupResult<PagedCollection<ListWord>>.ListNotFound();
        }

        var ownership = await _dbContext.UserLists
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(list => list.ListId == listId)
            .Select(list => new ListOwnership(
                list.ListId,
                list.UserId,
                list.Status,
                list.ListName))
            .SingleOrDefaultAsync(cancellationToken);

        if (ownership is null
            || ownership.Status == UserStatus.Deleted
            || PersonalTopicListName.IsReserved(ownership.ListName))
        {
            return ListLookupResult<PagedCollection<ListWord>>.ListNotFound();
        }

        if (ownership.UserId != userId)
        {
            return ListLookupResult<PagedCollection<ListWord>>.ListForbidden();
        }

        var query = _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId && listWord.ListId == listId)
            .OrderByDescending(listWord => listWord.AddedAt)
            .ThenByDescending(listWord => listWord.WordId)
            .Select(ListPersistenceMappings.ToListWord(_dbContext, userId));

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return ListLookupResult<PagedCollection<ListWord>>.Success(
            new PagedCollection<ListWord>(items, page, limit, totalItems));
    }
}
