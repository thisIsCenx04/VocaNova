using VocaNova.API.Features.Auth.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IUserProfileCache
{
    Task<UserProfileDto?> GetAsync(uint userId, CancellationToken cancellationToken = default);

    Task SetAsync(UserProfileDto profile, CancellationToken cancellationToken = default);

    Task RemoveAsync(uint userId, CancellationToken cancellationToken = default);
}
