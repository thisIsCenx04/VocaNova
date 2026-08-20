using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;
using VocaNova.API.Features.Auth.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using AuthUserProfile = VocaNova.API.Features.Auth.BLL.Models.UserProfile;
using PersistenceUserProfile = VocaNova.API.Infrastructure.Persistence.Entities.UserProfile;

namespace VocaNova.API.Features.Auth.DAL.Repositories;

public sealed class AuthAccountRepository : IAuthAccountRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AuthAccountRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthAccount?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        (await UserAggregate().SingleOrDefaultAsync(
            user => user.UserAuth != null && user.UserAuth.Phone == phone,
            cancellationToken))?.ToAuthAccount();

    public async Task<AuthAccount?> FindByGoogleSubjectAsync(
        string googleSubject,
        CancellationToken cancellationToken = default) =>
        (await UserAggregate().SingleOrDefaultAsync(
            user => user.UserAuth != null && user.UserAuth.GoogleUid == googleSubject,
            cancellationToken))?.ToAuthAccount();

    public async Task<AuthAccount?> FindByGoogleEmailAsync(
        string googleEmail,
        CancellationToken cancellationToken = default) =>
        (await UserAggregate().SingleOrDefaultAsync(
            user => user.UserAuth != null && user.UserAuth.GoogleEmail == googleEmail,
            cancellationToken))?.ToAuthAccount();

    public async Task<AuthAccount?> FindByIdAsync(uint userId, CancellationToken cancellationToken = default) =>
        (await UserAggregate().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken))?.ToAuthAccount();

    public async Task<AuthRole?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        (await _dbContext.Roles.SingleOrDefaultAsync(role => role.RoleName == roleName, cancellationToken))?.ToAuthRole();

    public Task StageCreateAsync(CreateAuthAccount account, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            RoleId = account.RoleId,
            Status = account.Status,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.CreatedAt,
            UserAuth = new UserAuth
            {
                Phone = account.Phone,
                PasswordHash = account.PasswordHash,
                IsPhoneVerified = account.IsPhoneVerified,
                GoogleUid = account.GoogleSubject,
                GoogleEmail = account.GoogleEmail,
                UpdatedAt = account.CreatedAt,
            },
                UserProfile = new PersistenceUserProfile
            {
                FullName = account.FullName ?? string.Empty,
                AvatarUrl = account.AvatarUrl,
                UpdatedAt = account.CreatedAt,
            },
        };

        if (account.LearningProfile is not null)
        {
            user.UserLearningProfile = new UserLearningProfile
            {
                AgeRangeId = account.LearningProfile.AgeRangeId,
                RegionId = account.LearningProfile.RegionId,
                OccupationId = account.LearningProfile.OccupationId,
                EducationLevelId = account.LearningProfile.EducationLevelId,
                LearningPurposeId = account.LearningProfile.LearningPurposeId,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.CreatedAt,
            };
        }

        _dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public async Task StageLastLoginAsync(uint userId, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleAsync(user => user.UserId == userId, cancellationToken);
        user.LastLoginAt = updatedAt;
        user.UpdatedAt = updatedAt;
    }

    public async Task<AuthUserProfile?> GetProfileAsync(uint userId, CancellationToken cancellationToken = default) =>
        (await UserAggregate().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken))?.ToUserProfile();

    public async Task<AuthUserProfile?> UpdateProfileAsync(
        uint userId,
        UpdateProfileCommand command,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await UserAggregate().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        EnsureProfile(user);
        user.UserProfile!.FullName = command.DisplayName!.Trim();
        user.UserProfile.AvatarUrl = string.IsNullOrWhiteSpace(command.AvatarUrl) ? null : command.AvatarUrl.Trim();
        user.UserProfile.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;
        return user.ToUserProfile();
    }

    public async Task<AuthUserProfile?> UpdateAvatarAsync(
        uint userId,
        string avatarUrl,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await UserAggregate().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        EnsureProfile(user);
        user.UserProfile!.AvatarUrl = avatarUrl;
        user.UserProfile.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;
        return user.ToUserProfile();
    }

    public async Task<AuthUserProfile?> UpsertLearningProfileAsync(
        uint userId,
        UpdateLearningProfileCommand command,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await UserAggregate().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.UserLearningProfile is null)
        {
            user.UserLearningProfile = new UserLearningProfile
            {
                UserId = user.UserId,
                CreatedAt = updatedAt,
            };
        }

        user.UserLearningProfile.AgeRangeId = command.AgeRangeId;
        user.UserLearningProfile.RegionId = command.RegionId;
        user.UserLearningProfile.OccupationId = command.OccupationId;
        user.UserLearningProfile.EducationLevelId = command.EducationLevelId;
        user.UserLearningProfile.LearningPurposeId = command.LearningPurposeId;
        user.UserLearningProfile.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;
        return user.ToUserProfile();
    }

    public Task<bool> ActiveAgeRangeExistsAsync(uint ageRangeId, CancellationToken cancellationToken = default) =>
        _dbContext.AgeRanges.AnyAsync(
            ageRange => ageRange.AgeRangeId == ageRangeId && ageRange.Status == UserStatus.Active,
            cancellationToken);

    public async Task<uint?> ResolveAgeRangeIdByAgeAsync(int age, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AgeRanges
            .AsNoTracking()
            .Where(ageRange => ageRange.Status == UserStatus.Active
                && (ageRange.MinAge == null || ageRange.MinAge <= age)
                && (ageRange.MaxAge == null || ageRange.MaxAge >= age))
            .OrderBy(ageRange => ageRange.DisplayOrder)
            .ThenBy(ageRange => ageRange.AgeRangeId)
            .Select(ageRange => (uint?)ageRange.AgeRangeId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ActiveRegionExistsAsync(uint regionId, CancellationToken cancellationToken = default) =>
        _dbContext.Regions.AnyAsync(region => region.RegionId == regionId && region.Status == UserStatus.Active, cancellationToken);

    public Task<bool> ActiveOccupationExistsAsync(uint occupationId, CancellationToken cancellationToken = default) =>
        _dbContext.Occupations.AnyAsync(occupation => occupation.OccupationId == occupationId && occupation.Status == UserStatus.Active, cancellationToken);

    public Task<bool> ActiveEducationLevelExistsAsync(uint educationLevelId, CancellationToken cancellationToken = default) =>
        _dbContext.EducationLevels.AnyAsync(
            educationLevel => educationLevel.EducationLevelId == educationLevelId && educationLevel.Status == UserStatus.Active,
            cancellationToken);

    public Task<bool> ActiveLearningPurposeExistsAsync(uint learningPurposeId, CancellationToken cancellationToken = default) =>
        _dbContext.LearningPurposes.AnyAsync(
            learningPurpose => learningPurpose.LearningPurposeId == learningPurposeId && learningPurpose.Status == UserStatus.Active,
            cancellationToken);

    public async Task UpdatePasswordAsync(
        uint userId,
        string passwordHash,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(user => user.UserAuth)
            .SingleAsync(user => user.UserId == userId, cancellationToken);
        if (user.UserAuth is null)
        {
            throw new InvalidOperationException("User auth is required to update password.");
        }

        user.UserAuth.PasswordHash = passwordHash;
        user.UserAuth.UpdatedAt = updatedAt;
        user.UpdatedAt = updatedAt;
    }

    public async Task<bool> StageSoftDeleteAsync(uint userId, DateTime now, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(user => user.UserAuth)
            .SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return false;
        }

        user.Status = UserStatus.Deleted;
        user.UpdatedAt = now;

        if (user.UserAuth is not null)
        {
            user.UserAuth.Phone = null;
            user.UserAuth.PasswordHash = null;
            user.UserAuth.IsPhoneVerified = false;
            user.UserAuth.GoogleUid = null;
            user.UserAuth.GoogleEmail = null;
            user.UserAuth.Username = null;
            user.UserAuth.UpdatedAt = now;
        }

        return true;
    }

    private IQueryable<User> UserAggregate() =>
        _dbContext.Users
            .Include(user => user.Role)
            .Include(user => user.UserAuth)
            .Include(user => user.UserProfile)
            .Include(user => user.UserLearningProfile);

    private static void EnsureProfile(User user)
    {
        if (user.UserProfile is null)
        {
            user.UserProfile = new PersistenceUserProfile
            {
                UserId = user.UserId,
            };
        }
    }
}
