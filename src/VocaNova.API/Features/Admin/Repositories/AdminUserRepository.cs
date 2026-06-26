using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
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
        else if (!query.IncludeDeleted)
        {
            // Mặc định ẩn user đã xóa; bật includeDeleted để xem cả deleted.
            source = source.Where(user => user.Status != UserStatus.Deleted);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            source = source.Where(user => user.Role.RoleName == query.Role);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            source = source.Where(user =>
                (user.UserAuth != null && user.UserAuth.Phone != null && user.UserAuth.Phone.ToLower().Contains(search))
                || (user.UserAuth != null && user.UserAuth.GoogleEmail != null && user.UserAuth.GoogleEmail.ToLower().Contains(search))
                || (user.UserProfile != null && user.UserProfile.FullName.ToLower().Contains(search)));
        }

        return source
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.UserId)
            .Select(user => new AdminUserSummaryDto(
                user.UserId,
                user.UserAuth == null ? null : user.UserAuth.Phone,
                user.UserAuth == null ? null : user.UserAuth.GoogleEmail,
                user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                user.UserProfile == null ? null : user.UserProfile.AvatarUrl,
                user.Role.RoleName,
                user.Status,
                user.LastLoginAt,
                user.CreatedAt))
            .ToPagedResultAsync(query.Page, query.Limit, cancellationToken);
    }

    public async Task<AdminUserTopicsDto> GetUserTopicsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var prefs = await _dbContext.UserTopicPreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == userId && pref.Status == UserStatus.Active)
            .OrderBy(pref => pref.Topic.TopicName)
            .Select(pref => new
            {
                pref.Source,
                pref.TopicId,
                Name = pref.Topic.TopicName,
                NameVi = pref.Topic.TopicNameVi,
            })
            .ToListAsync(cancellationToken);

        // source 'knn_suggested' = gợi ý AI; còn lại ('user_selected','onboarding') = user chọn.
        var selected = prefs
            .Where(p => p.Source != "knn_suggested")
            .Select(p => new AdminTopicChipDto(p.TopicId, p.Name, p.NameVi))
            .ToArray();
        var suggested = prefs
            .Where(p => p.Source == "knn_suggested")
            .Select(p => new AdminTopicChipDto(p.TopicId, p.Name, p.NameVi))
            .ToArray();

        return new AdminUserTopicsDto(selected, suggested);
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

    public Task<bool> UserExistsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(entity => entity.UserId == userId, cancellationToken);
    }

    public Task<PagedResult<AdminUserTestSessionDto>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TestSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.SessionId)
            .Select(session => new AdminUserTestSessionDto(
                session.SessionId,
                session.TestType,
                session.Mode,
                session.QuestionType,
                session.QuestionCount,
                session.CorrectCount,
                session.WrongCount,
                session.CorrectCount + session.WrongCount == 0
                    ? 0
                    : (float)session.CorrectCount / (session.CorrectCount + session.WrongCount) * 100,
                session.Score,
                session.MaxStreak,
                session.Status,
                session.StartedAt,
                session.EndedAt))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public Task<User?> FindUserForStatusUpdateAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .Include(entity => entity.Role)
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
