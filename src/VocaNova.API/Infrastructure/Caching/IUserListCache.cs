using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IUserListCache
{
    Task<IReadOnlyCollection<UserListDto>?> GetAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        uint userId,
        IReadOnlyCollection<UserListDto> lists,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
