using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AdminUserRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<AdminUserSummaryDto>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(user => user.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            source = source.Where(user =>
                (user.UserAuth != null && user.UserAuth.Phone != null && user.UserAuth.Phone.ToLower().Contains(search))
                || (user.UserProfile != null && user.UserProfile.FullName.ToLower().Contains(search)));
        }

        return source
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.UserId)
            .Select(user => new AdminUserSummaryDto(
                user.UserId,
                user.UserAuth == null ? null : user.UserAuth.Phone,
                user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                user.UserProfile == null ? null : user.UserProfile.AvatarUrl,
                user.Role.RoleName,
                user.Status,
                user.LastLoginAt,
                user.CreatedAt))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(entity => entity.Role)
            .Include(entity => entity.UserAuth)
            .Include(entity => entity.UserProfile)
            .Include(entity => entity.UserLearningProfile)
                .ThenInclude(profile => profile!.AgeRange)
            .Include(entity => entity.UserLearningProfile)
                .ThenInclude(profile => profile!.Region)
            .Include(entity => entity.UserLearningProfile)
                .ThenInclude(profile => profile!.Occupation)
            .Include(entity => entity.UserLearningProfile)
                .ThenInclude(profile => profile!.EducationLevel)
            .Include(entity => entity.UserLearningProfile)
                .ThenInclude(profile => profile!.LearningPurpose)
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);

        return user is null ? null : MapDetail(user);
    }

    public Task<User?> FindUserForStatusUpdateAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);
    }

    public async Task<int> RevokeActiveRefreshTokensAsync(
        uint userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
        }

        return tokens.Count;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AdminUserDetailDto MapDetail(User user)
    {
        var learningProfile = user.UserLearningProfile;
        return new AdminUserDetailDto(
            user.UserId,
            user.UserAuth?.Phone,
            user.UserAuth?.GoogleEmail,
            user.UserAuth?.Username,
            user.UserProfile?.FullName ?? string.Empty,
            user.UserProfile?.AvatarUrl,
            user.Role.RoleName,
            user.Status,
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt,
            learningProfile is null
                ? null
                : new AdminUserLearningProfileDto(
                    learningProfile.AgeRangeId,
                    learningProfile.AgeRange?.Name,
                    learningProfile.RegionId,
                    learningProfile.Region?.Name,
                    learningProfile.OccupationId,
                    learningProfile.Occupation?.Name,
                    learningProfile.EducationLevelId,
                    learningProfile.EducationLevel?.Name,
                    learningProfile.LearningPurposeId,
                    learningProfile.LearningPurpose?.Name));
    }
}
