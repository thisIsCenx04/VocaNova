using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Admin.BLL.Services.IServices;

namespace VocaNova.API.Features.Admin.BLL.Services;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly IReadOnlySet<string> SortColumns =
        new HashSet<string>(StringComparer.Ordinal) { "id", "name", "email", "status", "phone" };

    private readonly IAdminUserRepository _repository;
    private readonly IUserProfileCache? _userProfileCache;

    public AdminUserService(
        IAdminUserRepository repository,
        IUserProfileCache? userProfileCache = null)
    {
        _repository = repository;
        _userProfileCache = userProfileCache;
    }

    public async Task<Result<PagedResult<AdminUserSummaryModel>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<AdminUserSummaryModel>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminUserSummaryModel>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var status = NormalizeNullable(query.Status);
        if (status is not null && !UserStatus.All.Contains(status))
        {
            return Result<PagedResult<AdminUserSummaryModel>>.Fail("Status is invalid.");
        }

        var sortBy = NormalizeNullable(query.SortBy)?.ToLowerInvariant();
        if (sortBy is not null && !SortColumns.Contains(sortBy))
        {
            return Result<PagedResult<AdminUserSummaryModel>>.Fail("Sort column is invalid.");
        }

        var sortDirection = NormalizeNullable(query.SortDirection)?.ToLowerInvariant();
        if (sortDirection is not null && sortDirection is not ("asc" or "desc"))
        {
            return Result<PagedResult<AdminUserSummaryModel>>.Fail("Sort direction must be 'asc' or 'desc'.");
        }

        var normalized = query with
        {
            Status = status,
            Search = NormalizeNullable(query.Search),
            SortBy = sortBy,
            SortDirection = sortDirection,
        };

        var result = await _repository.GetUsersAsync(normalized, cancellationToken);
        return Result<PagedResult<AdminUserSummaryModel>>.Ok(result);
    }

    public async Task<Result<AdminUserDetailModel>> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<AdminUserDetailModel>.NotFound("User not found.");
        }

        var user = await _repository.GetUserDetailAsync(userId, cancellationToken);
        return user is null
            ? Result<AdminUserDetailModel>.NotFound("User not found.")
            : Result<AdminUserDetailModel>.Ok(user);
    }

    public async Task<Result<PagedResult<AdminUserTestSessionModel>>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0)
        {
            return Result<PagedResult<AdminUserTestSessionModel>>.Fail("Page must be greater than zero.");
        }

        if (limit <= 0 || limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminUserTestSessionModel>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        if (userId == 0 || !await _repository.UserExistsAsync(userId, cancellationToken))
        {
            return Result<PagedResult<AdminUserTestSessionModel>>.NotFound("User not found.");
        }

        var result = await _repository.GetTestHistoryAsync(userId, page, limit, cancellationToken);
        return Result<PagedResult<AdminUserTestSessionModel>>.Ok(result);
    }

    public async Task<Result<AdminUserTopicsModel>> GetUserTopicsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0 || !await _repository.UserExistsAsync(userId, cancellationToken))
        {
            return Result<AdminUserTopicsModel>.NotFound("User not found.");
        }

        var topics = await _repository.GetUserTopicsAsync(userId, cancellationToken);
        return Result<AdminUserTopicsModel>.Ok(topics);
    }

    public async Task<Result<bool>> DeactivateAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetStatusTargetAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("User not found.");
        }

        var guard = EnsureCanManageStatus(actorRole, user.RoleName);
        if (guard is not null)
        {
            return guard;
        }

        // "Disable" = khóa tài khoản (limit access): vẫn hiển thị trong danh sách với status 'locked',
        // không xóa/ẩn. Vẫn thu hồi refresh token để chặn truy cập ngay.
        var now = DateTime.UtcNow;
        if (!await _repository.StageStatusAsync(userId, UserStatus.Locked, now, cancellationToken))
        {
            return Result<bool>.NotFound("User not found.");
        }

        await _repository.RevokeActiveRefreshTokensAsync(userId, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetStatusTargetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.NotFound("User not found.");
        }

        var guard = EnsureCanManageStatus(actorRole, user.RoleName);
        if (guard is not null)
        {
            return guard;
        }

        // "Enable" = mở khóa: đưa user (locked hoặc deleted) về active.
        if (user.Status == UserStatus.Active)
        {
            return Result<bool>.Conflict("User is already active.");
        }

        if (!await _repository.StageStatusAsync(userId, UserStatus.Active, DateTime.UtcNow, cancellationToken))
        {
            return Result<bool>.NotFound("User not found.");
        }

        await _repository.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(userId, cancellationToken);

        return Result<bool>.Ok(true);
    }

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
