using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Abstractions;

public interface IUserListCache
{
    Task<IReadOnlyCollection<UserListSummary>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        IReadOnlyCollection<UserListSummary> lists,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
