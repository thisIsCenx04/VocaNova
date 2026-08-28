using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.DAL.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public AdminUserRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<AdminUserSummaryModel>> GetUsersAsync(
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

        var ordered = (query.SortBy, query.SortDirection) switch
        {
            ("id", "desc") => source.OrderByDescending(user => user.UserId),
            ("id", _) => source.OrderBy(user => user.UserId),
            ("name", "desc") => source.OrderByDescending(user => user.UserProfile == null ? string.Empty : user.UserProfile.FullName),
            ("name", _) => source.OrderBy(user => user.UserProfile == null ? string.Empty : user.UserProfile.FullName),
            ("email", "desc") => source.OrderByDescending(user => user.UserAuth == null ? string.Empty : user.UserAuth.GoogleEmail),
            ("email", _) => source.OrderBy(user => user.UserAuth == null ? string.Empty : user.UserAuth.GoogleEmail),
            ("status", "desc") => source.OrderByDescending(user => user.Status),
            ("status", _) => source.OrderBy(user => user.Status),
            ("phone", "desc") => source.OrderByDescending(user => user.UserAuth == null ? string.Empty : user.UserAuth.Phone),
            ("phone", _) => source.OrderBy(user => user.UserAuth == null ? string.Empty : user.UserAuth.Phone),
            _ => source.OrderByDescending(user => user.CreatedAt),
        };

        return ordered
            .ThenBy(user => user.UserId)
            .Select(user => new AdminUserSummaryModel(
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

    public async Task<AdminUserTopicsModel> GetUserTopicsAsync(
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
            .Select(p => new AdminTopicChipModel(p.TopicId, p.Name, p.NameVi))
            .ToArray();
        var suggested = prefs
            .Where(p => p.Source == "knn_suggested")
            .Select(p => new AdminTopicChipModel(p.TopicId, p.Name, p.NameVi))
            .ToArray();

        return new AdminUserTopicsModel(selected, suggested);
    }

    public async Task<AdminUserDetailModel?> GetUserDetailAsync(
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

    public Task<PagedResult<AdminUserTestSessionModel>> GetTestHistoryAsync(
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
            .Select(session => new AdminUserTestSessionModel(
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

    public Task<AdminUserStatusTarget?> GetStatusTargetAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .Select(entity => new AdminUserStatusTarget(
                entity.UserId,
                entity.Status,
                entity.Role == null ? null : entity.Role.RoleName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> StageStatusAsync(
        uint userId,
        string status,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.Status = status;
        user.UpdatedAt = updatedAt;
        return true;
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

    private static AdminUserDetailModel MapDetail(User user)
    {
        var learningProfile = user.UserLearningProfile;
        return new AdminUserDetailModel(
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
                : new AdminUserLearningProfileModel(
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
