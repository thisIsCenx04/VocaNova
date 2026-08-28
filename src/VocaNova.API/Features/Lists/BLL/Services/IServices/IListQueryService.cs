using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Services.IServices;

public interface IListQueryService
{
    Task<ListResult<IReadOnlyCollection<UserListSummary>>> GetListsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint listId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default);
}
