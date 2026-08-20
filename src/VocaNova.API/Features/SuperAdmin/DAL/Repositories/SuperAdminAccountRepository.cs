using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.SuperAdmin.DAL.Repositories;

public sealed class SuperAdminAccountRepository : ISuperAdminAccountRepository
{
    private static readonly IReadOnlySet<string> SortColumns =
        new HashSet<string>(StringComparer.Ordinal)
        { "id", "name", "email", "phone", "status", "created", "last_login" };

    private readonly VocaNovaDbContext _dbContext;

    public SuperAdminAccountRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AdminAccountModel>> GetAccountsAsync(AdminAccountQuery query, CancellationToken cancellationToken = default)
    {
        var status = Normalize(query.Status);
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

        var sortBy = Normalize(query.SortBy)?.ToLowerInvariant();
        var sortDirection = Normalize(query.SortDirection)?.ToLowerInvariant();

        var totalItems = await source.CountAsync(cancellationToken);
        var users = await ApplySort(source, sortBy, sortDirection == "desc")
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminAccountModel>(
            users.Select(Map).ToArray(), query.Page, query.Limit, totalItems);
    }

    public async Task<AdminAccountModel?> GetAccountAsync(uint adminId, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: false, cancellationToken);
        return admin is null ? null : Map(admin);
    }

    public async Task<string?> FindDuplicateAsync(string phone, string email, uint? excludedUserId, CancellationToken cancellationToken = default)
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

    public async Task<uint?> GetAdminRoleIdAsync(CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.RoleName == UserRole.Admin, cancellationToken);
        return role?.RoleId;
    }

    public async Task<AdminAccountModel> AddAccountAsync(CreateAdminAccountModel request, string passwordHash, uint roleId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var role = await _dbContext.Roles.FindAsync(new object[] { roleId }, cancellationToken);
        var admin = new User
        {
            RoleId = roleId,
            Role = role!,
            Status = Normalize(request.Status) ?? UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            UserAuth = new UserAuth
            {
                Phone = request.Phone!.Trim(),
                GoogleEmail = request.Email!.Trim().ToLowerInvariant(),
                IsPhoneVerified = true,
                PasswordHash = passwordHash,
                UpdatedAt = now,
            },
            UserProfile = new UserProfile
            {
                FullName = request.FullName!.Trim(),
                UpdatedAt = now,
            },
        };

        _dbContext.Users.Add(admin);
        return Map(admin);
    }

    public async Task<AdminAccountModel?> UpdateAccountAsync(uint adminId, UpdateAdminAccountModel request, string? passwordHash, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null) return null;

        var now = DateTime.UtcNow;
        admin.UserProfile!.FullName = request.FullName!.Trim();
        admin.UserProfile.UpdatedAt = now;
        admin.UserAuth!.Phone = request.Phone!.Trim();
        admin.UserAuth.GoogleEmail = request.Email!.Trim().ToLowerInvariant();
        admin.UserAuth.IsPhoneVerified = true;
        admin.UserAuth.UpdatedAt = now;
        
        if (passwordHash != null)
        {
            admin.UserAuth.PasswordHash = passwordHash;
        }

        var newStatus = Normalize(request.Status) ?? admin.Status;
        admin.Status = newStatus;
        admin.UpdatedAt = now;

        return Map(admin);
    }

    public async Task<bool> ChangeStatusAsync(uint adminId, string newStatus, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null) return false;

        var now = DateTime.UtcNow;
        admin.Status = newStatus;
        admin.UpdatedAt = now;
        return true;
    }

    public async Task<bool> DeleteAccountAsync(uint adminId, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: true, cancellationToken);
        if (admin is null) return false;

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
        return true;
    }

    public async Task RevokeActiveRefreshTokensAsync(uint adminId, DateTime now, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == adminId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);
            
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }

    public async Task<string?> GetAccountStatusAsync(uint adminId, CancellationToken cancellationToken = default)
    {
        var admin = await FindAdminAsync(adminId, tracking: false, cancellationToken);
        return admin?.Status;
    }

    private static IQueryable<User> ApplySort(IQueryable<User> source, string? sortBy, bool descending)
    {
        return sortBy switch
        {
            "id" => descending ? source.OrderByDescending(user => user.UserId) : source.OrderBy(user => user.UserId),
            "name" => descending ? source.OrderByDescending(user => user.UserProfile!.FullName) : source.OrderBy(user => user.UserProfile!.FullName),
            "email" => descending ? source.OrderByDescending(user => user.UserAuth!.GoogleEmail) : source.OrderBy(user => user.UserAuth!.GoogleEmail),
            "phone" => descending ? source.OrderByDescending(user => user.UserAuth!.Phone) : source.OrderBy(user => user.UserAuth!.Phone),
            "status" => descending ? source.OrderByDescending(user => user.Status) : source.OrderBy(user => user.Status),
            "created" => descending ? source.OrderByDescending(user => user.CreatedAt) : source.OrderBy(user => user.CreatedAt),
            "last_login" => descending
                ? source.OrderBy(user => user.LastLoginAt == null).ThenByDescending(user => user.LastLoginAt)
                : source.OrderBy(user => user.LastLoginAt == null).ThenBy(user => user.LastLoginAt),
            _ => source.OrderByDescending(user => user.CreatedAt),
        };
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

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static AdminAccountModel Map(User admin) => new(
        admin.UserId,
        admin.UserProfile?.FullName ?? string.Empty,
        admin.UserAuth?.GoogleEmail,
        admin.UserAuth?.Phone,
        admin.Role?.RoleName ?? string.Empty,
        admin.Status,
        admin.CreatedAt,
        admin.UpdatedAt,
        admin.LastLoginAt);
}
