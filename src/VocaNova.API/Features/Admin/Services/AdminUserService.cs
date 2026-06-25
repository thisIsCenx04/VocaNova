using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Features.Admin.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Admin.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _repository;
    private readonly IUserProfileCache? _userProfileCache;

    public AdminUserService(
        IAdminUserRepository repository,
        IUserProfileCache? userProfileCache = null)
    {
        _repository = repository;
        _userProfileCache = userProfileCache;
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

        var normalized = query with
        {
            Status = status,
            Search = NormalizeNullable(query.Search),
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

    public async Task<Result<bool>> DeactivateAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindUserForStatusUpdateAsync(userId, cancellationToken);
        if (user is null || user.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("User not found.");
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

    public async Task<Result<bool>> RestoreAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.FindUserForStatusUpdateAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.NotFound("User not found.");
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
