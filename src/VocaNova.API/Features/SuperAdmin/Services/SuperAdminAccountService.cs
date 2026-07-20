using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.SuperAdmin.Services;

public sealed class SuperAdminAccountService : ISuperAdminAccountService
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly IUserProfileCache? _userProfileCache;

    public SuperAdminAccountService(VocaNovaDbContext dbContext, IUserProfileCache? userProfileCache = null)
    {
        _dbContext = dbContext;
        _userProfileCache = userProfileCache;
    }

    public async Task<Result<PagedResult<AdminAccountDto>>> GetAccountsAsync(
        AdminAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminAccountDto>>.Fail("Paging parameters are invalid.");
        }

        var status = Normalize(query.Status);
        if (status is not null && !UserStatus.All.Contains(status))
        {
            return Result<PagedResult<AdminAccountDto>>.Fail("Status is invalid.");
        }

        var source = AdminAccounts().AsNoTracking();
        if (status is not null)
        {
            source = source.Where(user => user.Status == status);
        }
        else if (!query.IncludeDeleted)
        {
            source = source.Where(user => user.Status != UserStatus.Deleted);
        }

        var search = Normalize(query.Search);
        if (search is not null)
        {
            source = source.Where(user =>
                user.UserProfile!.FullName.Contains(search)
                || (user.UserAuth!.GoogleEmail != null && user.UserAuth.GoogleEmail.Contains(search))
                || (user.UserAuth.Phone != null && user.UserAuth.Phone.Contains(search)));
        }

        var totalItems = await source.CountAsync(cancellationToken);
        var users = await source
            .OrderByDescending(user => user.CreatedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AdminAccountDto>>.Ok(new PagedResult<AdminAccountDto>(
            users.Select(Map).ToArray(), query.Page, query.Limit, totalItems));
    }

    public async Task<Result<AdminAccountDto>> GetAccountAsync(
        uint adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: false, cancellationToken);
        return admin is null
            ? Result<AdminAccountDto>.NotFound("Admin account not found.")
            : Result<AdminAccountDto>.Ok(Map(admin));
    }

    public async Task<Result<AdminAccountDto>> CreateAsync(
        CreateAdminAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        var duplicate = await FindDuplicateAsync(phone, email, null, cancellationToken);
        if (duplicate is not null)
        {
            return Result<AdminAccountDto>.Conflict(duplicate);
        }

        var role = await _dbContext.Roles.SingleOrDefaultAsync(
            item => item.RoleName == UserRole.Admin,
            cancellationToken);
        if (role is null)
        {
            return Result<AdminAccountDto>.Fail("Admin role is not configured.");
        }

        var now = DateTime.UtcNow;
        var admin = new User
        {
            RoleId = role.RoleId,
            Role = role,
            Status = Normalize(request.Status) ?? UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            UserAuth = new UserAuth
            {
                Phone = phone,
                GoogleEmail = email,
                IsPhoneVerified = true,
                PasswordHash = PasswordHelper.Hash(request.Password!),
                UpdatedAt = now,
            },
            UserProfile = new UserProfile
            {
                FullName = request.FullName!.Trim(),
                UpdatedAt = now,
            },
        };

        _dbContext.Users.Add(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<AdminAccountDto>.Ok(Map(admin));
    }

    public async Task<Result<AdminAccountDto>> UpdateAsync(
        uint adminId,
        UpdateAdminAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null || admin.Status == UserStatus.Deleted)
        {
            return Result<AdminAccountDto>.NotFound("Admin account not found.");
        }

        var phone = request.Phone!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        var duplicate = await FindDuplicateAsync(phone, email, adminId, cancellationToken);
        if (duplicate is not null)
        {
            return Result<AdminAccountDto>.Conflict(duplicate);
        }

        var now = DateTime.UtcNow;
        admin.UserProfile!.FullName = request.FullName!.Trim();
        admin.UserProfile.UpdatedAt = now;
        admin.UserAuth!.Phone = phone;
        admin.UserAuth.GoogleEmail = email;
        admin.UserAuth.IsPhoneVerified = true;
        admin.UserAuth.UpdatedAt = now;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            admin.UserAuth.PasswordHash = PasswordHelper.Hash(request.Password);
            await RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
        }

        var newStatus = Normalize(request.Status) ?? admin.Status;
        if (newStatus == UserStatus.Locked && admin.Status != UserStatus.Locked)
        {
            await RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
        }

        admin.Status = newStatus;
        admin.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(adminId, cancellationToken);
        return Result<AdminAccountDto>.Ok(Map(admin));
    }

    public Task<Result<bool>> LockAsync(uint adminId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(adminId, UserStatus.Locked, cancellationToken);

    public Task<Result<bool>> UnlockAsync(uint adminId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(adminId, UserStatus.Active, cancellationToken);

    public async Task<Result<bool>> DeleteAsync(uint adminId, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null || admin.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("Admin account not found.");
        }

        var now = DateTime.UtcNow;
        admin.Status = UserStatus.Deleted;
        admin.UpdatedAt = now;
        admin.UserAuth!.Phone = null;
        admin.UserAuth.GoogleEmail = null;
        admin.UserAuth.GoogleUid = null;
        admin.UserAuth.Username = null;
        admin.UserAuth.PasswordHash = null;
        admin.UserAuth.IsPhoneVerified = false;
        admin.UserAuth.UpdatedAt = now;
        await RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(adminId, cancellationToken);
        return Result<bool>.Ok(true);
    }

    private async Task<Result<bool>> ChangeStatusAsync(
        uint adminId,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null || admin.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("Admin account not found.");
        }

        if (admin.Status == newStatus)
        {
            return Result<bool>.Conflict($"Admin account is already {newStatus}.");
        }

        var now = DateTime.UtcNow;
        admin.Status = newStatus;
        admin.UpdatedAt = now;
        if (newStatus == UserStatus.Locked)
        {
            await RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RemoveCachedProfileAsync(adminId, cancellationToken);
        return Result<bool>.Ok(true);
    }

    private IQueryable<User> AdminAccounts() => _dbContext.Users
        .Include(user => user.Role)
        .Include(user => user.UserAuth)
        .Include(user => user.UserProfile)
        .Where(user => user.Role.RoleName == UserRole.Admin);

    private Task<User?> FindAdminAsync(uint adminId, bool tracking, CancellationToken cancellationToken)
    {
        var query = AdminAccounts();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(user => user.UserId == adminId, cancellationToken);
    }

    private async Task<string?> FindDuplicateAsync(
        string phone,
        string email,
        uint? excludedUserId,
        CancellationToken cancellationToken)
    {
        var source = _dbContext.UserAuths.AsNoTracking()
            .Where(auth => !excludedUserId.HasValue || auth.UserId != excludedUserId.Value);
        if (await source.AnyAsync(auth => auth.Phone == phone, cancellationToken))
        {
            return "Phone already exists.";
        }

        return await source.AnyAsync(auth => auth.GoogleEmail == email, cancellationToken)
            ? "Email already exists."
            : null;
    }

    private async Task RevokeActiveRefreshTokensAsync(uint adminId, DateTime now, CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == adminId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }

    private Task RemoveCachedProfileAsync(uint adminId, CancellationToken cancellationToken) =>
        _userProfileCache?.RemoveAsync(adminId, cancellationToken) ?? Task.CompletedTask;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static AdminAccountDto Map(User admin) => new(
        admin.UserId,
        admin.UserProfile?.FullName ?? string.Empty,
        admin.UserAuth?.GoogleEmail,
        admin.UserAuth?.Phone,
        admin.Role.RoleName,
        admin.Status,
        admin.CreatedAt,
        admin.UpdatedAt,
        admin.LastLoginAt);
}
