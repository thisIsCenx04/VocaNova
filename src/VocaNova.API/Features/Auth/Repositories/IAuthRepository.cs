using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.Repositories;

public interface IAuthRepository
{
    Task<User?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<User?> FindByGoogleUidAsync(string googleUid, CancellationToken cancellationToken = default);

    Task<User> CreateUserAsync(
        User user,
        UserAuth userAuth,
        UserProfile userProfile,
        UserLearningProfile? learningProfile = null,
        CancellationToken cancellationToken = default);

    Task<RefreshToken> CreateRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeTokenAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);
}
