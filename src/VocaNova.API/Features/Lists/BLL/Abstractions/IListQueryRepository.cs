using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Abstractions;

public interface IListQueryRepository
{
    Task<IReadOnlyCollection<UserListSummary>> GetListsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<ListLookupResult<PagedCollection<ListWord>>> GetOwnedListWordsAsync(
        uint userId,
        uint listId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
