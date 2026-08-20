using Microsoft.EntityFrameworkCore;

namespace VocaNova.API.Features.Auth.DAL.Repositories;

internal sealed class AuthRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AuthRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        UserAggregate().SingleOrDefaultAsync(
            user => user.UserAuth != null && user.UserAuth.Phone == phone,
            cancellationToken);

    public Task<User?> FindByGoogleUidAsync(string googleUid, CancellationToken cancellationToken = default) =>
        UserAggregate().SingleOrDefaultAsync(
            user => user.UserAuth != null && user.UserAuth.GoogleUid == googleUid,
            cancellationToken);

    public async Task CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeTokenAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens.SingleOrDefaultAsync(
            refreshToken => refreshToken.TokenHash == tokenHash,
            cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.RevokedAt = revokedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<User> CreateUserAsync(
        User user,
        UserAuth userAuth,
        UserProfile userProfile,
        UserLearningProfile? userLearningProfile,
        CancellationToken cancellationToken = default)
    {
        user.UserAuth = userAuth;
        user.UserProfile = userProfile;
        user.UserLearningProfile = userLearningProfile;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private IQueryable<User> UserAggregate() =>
        _dbContext.Users
            .Include(user => user.Role)
            .Include(user => user.UserAuth)
            .Include(user => user.UserProfile)
            .Include(user => user.UserLearningProfile);
}
