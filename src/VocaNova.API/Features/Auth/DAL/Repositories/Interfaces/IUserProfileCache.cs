using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IUserProfileCache
{
    Task<UserProfile?> GetAsync(uint userId, CancellationToken cancellationToken = default);

    Task SetAsync(UserProfile profile, CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
