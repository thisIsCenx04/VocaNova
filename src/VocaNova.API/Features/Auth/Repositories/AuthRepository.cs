using Microsoft.EntityFrameworkCore;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Auth.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AuthRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return UserAggregate()
            .SingleOrDefaultAsync(
                user => user.UserAuth != null && user.UserAuth.Phone == phone,
                cancellationToken);
    }

    public Task<User?> FindByGoogleUidAsync(string googleUid, CancellationToken cancellationToken = default)
    {
        return UserAggregate()
            .SingleOrDefaultAsync(
                user => user.UserAuth != null && user.UserAuth.GoogleUid == googleUid,
                cancellationToken);
    }

    public Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles
            .SingleOrDefaultAsync(role => role.RoleName == roleName, cancellationToken);
    }

    public async Task<User> CreateUserAsync(
        User user,
        UserAuth userAuth,
        UserProfile userProfile,
        UserLearningProfile? learningProfile = null,
        CancellationToken cancellationToken = default)
    {
        user.UserAuth = userAuth;
        user.UserProfile = userProfile;

        if (learningProfile is not null)
        {
            user.UserLearningProfile = learningProfile;
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<bool> RevokeTokenAsync(
        string tokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return false;
        }

        refreshToken.RevokedAt = revokedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private IQueryable<User> UserAggregate()
    {
        return _dbContext.Users
            .Include(user => user.Role)
            .Include(user => user.UserAuth)
            .Include(user => user.UserProfile)
            .Include(user => user.UserLearningProfile);
    }
}
