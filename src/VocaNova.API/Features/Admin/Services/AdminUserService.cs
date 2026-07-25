using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Features.SuperAdmin.Services;

namespace VocaNova.API.Features.Admin.Services;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly IReadOnlySet<string> SortColumns =
        new HashSet<string>(StringComparer.Ordinal) { "id", "name", "email", "status", "phone" };

    private readonly IAdminUserRepository _repository;
    private readonly IUserProfileCache? _userProfileCache;
    private readonly IAdminUserAssignmentStore? _assignmentStore;

    public AdminUserService(
        IAdminUserRepository repository,
        IUserProfileCache? userProfileCache = null,
        IAdminUserAssignmentStore? assignmentStore = null)
    {
        _repository = repository;
        _userProfileCache = userProfileCache;
        _assignmentStore = assignmentStore;
    }

    public async Task<Result<PagedResult<AdminUserSummaryDto>>> GetUsersAsync(
        AdminUserQuery query, uint actorId, string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(actorRole, UserRole.SuperAdmin, StringComparison.Ordinal))
            return await GetUsersAsync(query, cancellationToken);
        if (_assignmentStore is null)
            return Result<PagedResult<AdminUserSummaryDto>>.Forbidden("Admin assignment storage is unavailable.");

        var validation = ValidateQuery(query);
        if (validation is not null) return validation;
        var normalized = NormalizeQuery(query);
        var userIds = await _assignmentStore.GetUserIdsAsync(actorId, cancellationToken);
        var result = await _repository.GetUsersAsync(normalized, cancellationToken, userIds);
        return Result<PagedResult<AdminUserSummaryDto>>.Ok(result);
    }

    public async Task<Result<PagedResult<AdminUserSummaryDto>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminUserSummaryDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var status = NormalizeNullable(query.Status);
        if (status is not null && !UserStatus.All.Contains(status))
        {
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Status is invalid.");
        }

        var sortBy = NormalizeNullable(query.SortBy)?.ToLowerInvariant();
        if (sortBy is not null && !SortColumns.Contains(sortBy))
        {
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Sort column is invalid.");
        }

        var sortDirection = NormalizeNullable(query.SortDirection)?.ToLowerInvariant();
        if (sortDirection is not null && sortDirection is not ("asc" or "desc"))
        {
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Sort direction must be 'asc' or 'desc'.");
        }

        var normalized = query with
        {
            Status = status,
            Search = NormalizeNullable(query.Search),
            SortBy = sortBy,
            SortDirection = sortDirection,
        };

        var result = await _repository.GetUsersAsync(normalized, cancellationToken);
        return Result<PagedResult<AdminUserSummaryDto>>.Ok(result);
    }

    public async Task<Result<AdminUserDetailDto>> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<AdminUserDetailDto>.NotFound("User not found.");
        }

        var user = await _repository.GetUserDetailAsync(userId, cancellationToken);
        return user is null
            ? Result<AdminUserDetailDto>.NotFound("User not found.")
            : Result<AdminUserDetailDto>.Ok(user);
    }

    public async Task<Result<AdminUserDetailDto>> GetUserDetailAsync(
        uint userId, uint actorId, string actorRole, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessAsync(userId, actorId, actorRole, cancellationToken))
            return Result<AdminUserDetailDto>.Forbidden("You can only manage users assigned to your account.");
        return await GetUserDetailAsync(userId, cancellationToken);
    }

    public async Task<Result<PagedResult<AdminUserTestSessionDto>>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0)
        {
            return Result<PagedResult<AdminUserTestSessionDto>>.Fail("Page must be greater than zero.");
        }

        if (limit <= 0 || limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminUserTestSessionDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        if (userId == 0 || !await _repository.UserExistsAsync(userId, cancellationToken))
        {
            return Result<PagedResult<AdminUserTestSessionDto>>.NotFound("User not found.");
        }

        var result = await _repository.GetTestHistoryAsync(userId, page, limit, cancellationToken);
        return Result<PagedResult<AdminUserTestSessionDto>>.Ok(result);
    }

    public async Task<Result<PagedResult<AdminUserTestSessionDto>>> GetTestHistoryAsync(
        uint userId, int page, int limit, uint actorId, string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessAsync(userId, actorId, actorRole, cancellationToken))
            return Result<PagedResult<AdminUserTestSessionDto>>.Forbidden("You can only manage users assigned to your account.");
        return await GetTestHistoryAsync(userId, page, limit, cancellationToken);
    }

    public async Task<Result<AdminUserTopicsDto>> GetUserTopicsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0 || !await _repository.UserExistsAsync(userId, cancellationToken))
        {
            return Result<AdminUserTopicsDto>.NotFound("User not found.");
        }

        var topics = await _repository.GetUserTopicsAsync(userId, cancellationToken);
        return Result<AdminUserTopicsDto>.Ok(topics);
    }

    public async Task<Result<AdminUserTopicsDto>> GetUserTopicsAsync(
        uint userId, uint actorId, string actorRole, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessAsync(userId, actorId, actorRole, cancellationToken))
            return Result<AdminUserTopicsDto>.Forbidden("You can only manage users assigned to your account.");
        return await GetUserTopicsAsync(userId, cancellationToken);
    }

    public async Task<Result<bool>> DeactivateAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindUserForStatusUpdateAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("User not found.");
        }

        var guard = EnsureCanManageStatus(actorRole, user.Role?.RoleName);
        if (guard is not null)
        {
            return guard;
        }

        // "Disable" = khóa tài khoản (limit access): vẫn hiển thị trong danh sách với status 'locked',
        // không xóa/ẩn. Vẫn thu hồi refresh token để chặn truy cập ngay.
        var now = DateTime.UtcNow;
        user.Status = UserStatus.Locked;
        user.UpdatedAt = now;
        await _repository.RevokeActiveRefreshTokensAsync(userId, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeactivateAsync(
        uint userId, uint actorId, string actorRole, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessAsync(userId, actorId, actorRole, cancellationToken))
            return Result<bool>.Forbidden("You can only manage users assigned to your account.");
        return await DeactivateAsync(userId, actorRole, cancellationToken);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindUserForStatusUpdateAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.NotFound("User not found.");
        }

        var guard = EnsureCanManageStatus(actorRole, user.Role?.RoleName);
        if (guard is not null)
        {
            return guard;
        }

        // "Enable" = mở khóa: đưa user (locked hoặc deleted) về active.
        if (user.Status == UserStatus.Active)
        {
            return Result<bool>.Conflict("User is already active.");
        }

        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint userId, uint actorId, string actorRole, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessAsync(userId, actorId, actorRole, cancellationToken))
            return Result<bool>.Forbidden("You can only manage users assigned to your account.");
        return await RestoreAsync(userId, actorRole, cancellationToken);
    }

    private async Task<bool> CanAccessAsync(
        uint userId, uint actorId, string actorRole, CancellationToken cancellationToken)
    {
        if (string.Equals(actorRole, UserRole.SuperAdmin, StringComparison.Ordinal)) return true;
        if (_assignmentStore is null) return false;
        return (await _assignmentStore.GetUserIdsAsync(actorId, cancellationToken)).Contains(userId);
    }

    private static Result<PagedResult<AdminUserSummaryDto>>? ValidateQuery(AdminUserQuery query)
    {
        if (query.Page <= 0) return Result<PagedResult<AdminUserSummaryDto>>.Fail("Page must be greater than zero.");
        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
            return Result<PagedResult<AdminUserSummaryDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        var status = NormalizeNullable(query.Status);
        if (status is not null && !UserStatus.All.Contains(status))
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Status is invalid.");
        var sortBy = NormalizeNullable(query.SortBy)?.ToLowerInvariant();
        if (sortBy is not null && !SortColumns.Contains(sortBy))
            return Result<PagedResult<AdminUserSummaryDto>>.Fail("Sort column is invalid.");
        var direction = NormalizeNullable(query.SortDirection)?.ToLowerInvariant();
        return direction is not null && direction is not ("asc" or "desc")
            ? Result<PagedResult<AdminUserSummaryDto>>.Fail("Sort direction must be 'asc' or 'desc'.")
            : null;
    }

    private static AdminUserQuery NormalizeQuery(AdminUserQuery query) => query with
    {
        Status = NormalizeNullable(query.Status),
        Search = NormalizeNullable(query.Search),
        SortBy = NormalizeNullable(query.SortBy)?.ToLowerInvariant(),
        SortDirection = NormalizeNullable(query.SortDirection)?.ToLowerInvariant(),
    };

    // Phân quyền khóa/mở tài khoản theo vai trò đối tượng:
    // - super_admin: không khóa/mở (bảo vệ tài khoản super admin).
    // - admin: chỉ super_admin mới thao tác được.
    // - user thường: admin hoặc super_admin đều được.
    private static Result<bool>? EnsureCanManageStatus(string actorRole, string? targetRole)
    {
        if (string.Equals(targetRole, UserRole.SuperAdmin, StringComparison.Ordinal))
        {
            return Result<bool>.Forbidden("Không thể thay đổi trạng thái của tài khoản super admin.");
        }

        if (string.Equals(targetRole, UserRole.Admin, StringComparison.Ordinal)
            && !string.Equals(actorRole, UserRole.SuperAdmin, StringComparison.Ordinal))
        {
            return Result<bool>.Forbidden("Chỉ super admin mới được khóa/mở tài khoản admin.");
        }

        return null;
    }

    private async Task RemoveCachedProfileAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_userProfileCache is not null)
        {
            await _userProfileCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
