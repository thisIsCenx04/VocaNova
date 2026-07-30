using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
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

    public Task<User?> FindByGoogleEmailAsync(string googleEmail, CancellationToken cancellationToken = default)
    {
        return UserAggregate()
            .SingleOrDefaultAsync(
                user => user.UserAuth != null && user.UserAuth.GoogleEmail == googleEmail,
                cancellationToken);
    }

    public Task<User?> FindByIdAsync(uint userId, CancellationToken cancellationToken = default)
    {
        return UserAggregate()
            .SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
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

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(
        RefreshToken refreshToken,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        refreshToken.RevokedAt = revokedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserProfileAsync(
        User user,
        string displayName,
        string? avatarUrl,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        if (user.UserProfile is null)
        {
            user.UserProfile = new UserProfile
            {
                UserId = user.UserId,
            };
        }

        user.UserProfile.FullName = displayName;
        user.UserProfile.AvatarUrl = avatarUrl;
        user.UserProfile.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserLearningProfile> UpsertLearningProfileAsync(
        User user,
        uint? ageRangeId,
        uint? regionId,
        uint? occupationId,
        uint? educationLevelId,
        uint? learningPurposeId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        if (user.UserLearningProfile is null)
        {
            user.UserLearningProfile = new UserLearningProfile
            {
                UserId = user.UserId,
                CreatedAt = updatedAt,
            };
        }

        user.UserLearningProfile.AgeRangeId = ageRangeId;
        user.UserLearningProfile.RegionId = regionId;
        user.UserLearningProfile.OccupationId = occupationId;
        user.UserLearningProfile.EducationLevelId = educationLevelId;
        user.UserLearningProfile.LearningPurposeId = learningPurposeId;
        user.UserLearningProfile.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.UserLearningProfile;
    }

    public Task<bool> ActiveAgeRangeExistsAsync(uint ageRangeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.AgeRanges.AnyAsync(
            ageRange => ageRange.AgeRangeId == ageRangeId && ageRange.Status == UserStatus.Active,
            cancellationToken);
    }

    public async Task<uint?> ResolveAgeRangeIdByAgeAsync(int age, CancellationToken cancellationToken = default)
    {
        var matches = await _dbContext.AgeRanges
            .AsNoTracking()
            .Where(ageRange => ageRange.Status == UserStatus.Active
                && (ageRange.MinAge == null || ageRange.MinAge <= age)
                && (ageRange.MaxAge == null || ageRange.MaxAge >= age))
            .OrderBy(ageRange => ageRange.DisplayOrder)
            .ThenBy(ageRange => ageRange.AgeRangeId)
            .Select(ageRange => (uint?)ageRange.AgeRangeId)
            .FirstOrDefaultAsync(cancellationToken);

        return matches;
    }

    public Task<bool> ActiveRegionExistsAsync(uint regionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Regions.AnyAsync(
            region => region.RegionId == regionId && region.Status == UserStatus.Active,
            cancellationToken);
    }

    public Task<bool> ActiveOccupationExistsAsync(uint occupationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Occupations.AnyAsync(
            occupation => occupation.OccupationId == occupationId && occupation.Status == UserStatus.Active,
            cancellationToken);
    }

    public Task<bool> ActiveEducationLevelExistsAsync(uint educationLevelId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EducationLevels.AnyAsync(
            educationLevel => educationLevel.EducationLevelId == educationLevelId
                && educationLevel.Status == UserStatus.Active,
            cancellationToken);
    }

    public Task<bool> ActiveLearningPurposeExistsAsync(uint learningPurposeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LearningPurposes.AnyAsync(
            learningPurpose => learningPurpose.LearningPurposeId == learningPurposeId
                && learningPurpose.Status == UserStatus.Active,
            cancellationToken);
    }

    public Task<OtpVerification?> FindLatestOtpByPhoneAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OtpVerifications
            .Where(otp => otp.Phone == phone && otp.Status == OtpStatus.Active)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OtpVerification?> FindLatestOtpByPhoneAndUserAsync(
        string phone,
        uint? userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OtpVerifications
            .Where(otp => otp.Phone == phone
                && otp.UserId == userId
                && otp.Status == OtpStatus.Active)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OtpVerification?> FindLatestOtpByPhoneSinceAsync(
        string phone,
        DateTime createdSince,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OtpVerifications
            .Where(otp => otp.Phone == phone && otp.CreatedAt >= createdSince)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OtpVerification> CreateOtpAsync(
        OtpVerification otpVerification,
        CancellationToken cancellationToken = default)
    {
        _dbContext.OtpVerifications.Add(otpVerification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return otpVerification;
    }

    public async Task UpdatePasswordAsync(
        User user,
        string passwordHash,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        if (user.UserAuth is null)
        {
            throw new InvalidOperationException("User auth is required to update password.");
        }

        user.UserAuth.PasswordHash = passwordHash;
        user.UserAuth.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
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
