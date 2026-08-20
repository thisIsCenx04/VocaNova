using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.SuperAdmin.DAL.Repositories;

public sealed class RoleManagementRepository : IRoleManagementRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public RoleManagementRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<RoleModel>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Roles.AsNoTracking()
            .OrderBy(role => role.RoleId)
            .Select(role => new RoleModel(role.RoleId, role.RoleName))
            .ToListAsync(cancellationToken);

        IEnumerable<RoleModel> source = rows;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(role => role.RoleName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        
        var type = query.Type?.Trim().ToLowerInvariant();
        if (type == "system") source = source.Where(role => UserRole.All.Contains(role.RoleName));
        else if (type == "custom") source = source.Where(role => !UserRole.All.Contains(role.RoleName));

        var sortBy = query.SortBy?.Trim().ToLowerInvariant();
        var sortDirection = query.SortDirection?.Trim().ToLowerInvariant();

        source = SortRoles(source, sortBy, sortDirection == "desc");

        var filtered = source.ToArray();
        var total = filtered.Length;
        var roles = filtered
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToArray();
            
        return new PagedResult<RoleModel>(roles, query.Page, query.Limit, total);
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AnyAsync(role => role.RoleName == roleName, cancellationToken);

    public Task<bool> RoleExistsExcludeAsync(uint roleId, string roleName, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AnyAsync(item => item.RoleId != roleId && item.RoleName == roleName, cancellationToken);

    public Task<RoleModel> AddRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = new Role { RoleName = roleName };
        _dbContext.Roles.Add(role);
        return Task.FromResult(new RoleModel(role.RoleId, role.RoleName)); // Note: RoleId may be 0 until SaveChanges is called by the caller
    }

    public async Task<RoleModel?> UpdateRoleAsync(uint roleId, string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        if (role is null) return null;

        role.RoleName = roleName;
        return new RoleModel(role.RoleId, role.RoleName);
    }

    public async Task<bool> DeleteRoleAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        if (role is null) return false;
        
        _dbContext.Roles.Remove(role);
        return true;
    }

    public async Task<IReadOnlyCollection<RoleUserModel>?> GetRoleUsersAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Roles.AnyAsync(role => role.RoleId == roleId, cancellationToken))
            return null;
            
        var users = await _dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.RoleId == roleId)
            .OrderBy(user => user.UserProfile!.FullName)
            .Select(user => new RoleUserModel(
                user.UserId, 
                user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                user.UserAuth == null ? null : user.UserAuth.GoogleEmail,
                user.UserAuth == null ? null : user.UserAuth.Phone, 
                user.Status))
            .ToListAsync(cancellationToken);
            
        return users;
    }

    public Task<bool> RoleIdExistsAsync(uint roleId, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.AnyAsync(role => role.RoleId == roleId, cancellationToken);

    public async Task<string?> GetRoleNameAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        return role?.RoleName;
    }

    public async Task<string?> GetUserRoleNameAsync(uint userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return user?.Role?.RoleName;
    }

    public async Task<bool> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null) return false;

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public async Task<bool> RemoveRoleAsync(uint roleId, uint userId, uint defaultRoleId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null) return false;

        user.RoleId = defaultRoleId;
        user.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public async Task<uint?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(item => item.RoleName == roleName, cancellationToken);
        return role?.RoleId;
    }

    public Task<bool> HasUsersWithRoleAsync(uint roleId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(user => user.RoleId == roleId, cancellationToken);

    public async Task RevokeActiveRefreshTokensAsync(uint userId, DateTime now, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAt = now;
    }

    private static IEnumerable<RoleModel> SortRoles(
        IEnumerable<RoleModel> source,
        string? sortBy,
        bool descending)
    {
        if (string.IsNullOrEmpty(sortBy)) return source;

        if (sortBy == "id")
        {
            return descending
                ? source.OrderByDescending(role => role.RoleId)
                : source.OrderBy(role => role.RoleId);
        }

        if (sortBy == "type")
        {
            return descending
                ? source.OrderByDescending(role => UserRole.All.Contains(role.RoleName))
                    .ThenBy(role => role.RoleName, StringComparer.OrdinalIgnoreCase)
                : source.OrderBy(role => UserRole.All.Contains(role.RoleName))
                    .ThenBy(role => role.RoleName, StringComparer.OrdinalIgnoreCase);
        }

        return descending
            ? source.OrderByDescending(role => role.RoleName, StringComparer.OrdinalIgnoreCase)
            : source.OrderBy(role => role.RoleName, StringComparer.OrdinalIgnoreCase);
    }
}
