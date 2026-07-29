using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.SuperAdmin.Services;

public sealed partial class RoleManagementService : IRoleManagementService
{
    private readonly VocaNovaDbContext _dbContext;
    private readonly IUserProfileCache? _profileCache;

    public RoleManagementService(VocaNovaDbContext dbContext, IUserProfileCache? profileCache = null)
    {
        _dbContext = dbContext;
        _profileCache = profileCache;
    }

    public async Task<Result<PagedResult<RoleDto>>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
            return Result<PagedResult<RoleDto>>.Fail("Paging parameters are invalid.");

        var rows = await _dbContext.Roles.AsNoTracking()
            .OrderBy(role => role.RoleId)
            .Select(role => new RoleDto(role.RoleId, role.RoleName))
            .ToListAsync(cancellationToken);

        IEnumerable<RoleDto> source = rows;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(role => role.RoleName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        var type = query.Type?.Trim().ToLowerInvariant();
        if (type == "system") source = source.Where(role => UserRole.All.Contains(role.RoleName));
        else if (type == "custom") source = source.Where(role => !UserRole.All.Contains(role.RoleName));
        else if (type is not null and not "") return Result<PagedResult<RoleDto>>.Fail("Role type is invalid.");

        var filtered = source.ToArray();
        var total = filtered.Length;
        var roles = filtered
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToArray();
        return Result<PagedResult<RoleDto>>.Ok(new PagedResult<RoleDto>(roles, query.Page, query.Limit, total));
    }

    public async Task<Result<RoleDto>> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null) return Result<RoleDto>.Fail(validation);
        var name = NormalizeName(request.RoleName!);
        if (await _dbContext.Roles.AnyAsync(role => role.RoleName == name, cancellationToken))
            return Result<RoleDto>.Conflict("Role name already exists.");

        var role = new Role { RoleName = name };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<RoleDto>.Ok(Map(role));
    }

    public async Task<Result<RoleDto>> UpdateAsync(uint roleId, SaveRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null) return Result<RoleDto>.Fail(validation);
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        if (role is null) return Result<RoleDto>.NotFound("Role not found.");

        var name = NormalizeName(request.RoleName!);
        if (UserRole.All.Contains(role.RoleName) && !string.Equals(role.RoleName, name, StringComparison.Ordinal))
            return Result<RoleDto>.Forbidden("System role names cannot be changed.");
        if (await _dbContext.Roles.AnyAsync(item => item.RoleId != roleId && item.RoleName == name, cancellationToken))
            return Result<RoleDto>.Conflict("Role name already exists.");

        role.RoleName = name;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<RoleDto>.Ok(Map(role));
    }

    public async Task<Result<bool>> DeleteAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        if (role is null) return Result<bool>.NotFound("Role not found.");
        if (UserRole.All.Contains(role.RoleName))
            return Result<bool>.Forbidden("System roles cannot be deleted.");
        if (await _dbContext.Users.AnyAsync(user => user.RoleId == roleId, cancellationToken))
            return Result<bool>.Conflict("Role is still assigned to one or more users.");
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<IReadOnlyCollection<RoleUserDto>>> GetUsersAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Roles.AnyAsync(role => role.RoleId == roleId, cancellationToken))
            return Result<IReadOnlyCollection<RoleUserDto>>.NotFound("Role not found.");
        var users = await _dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.RoleId == roleId)
            .OrderBy(user => user.UserProfile!.FullName)
            .Select(user => new RoleUserDto(user.UserId, user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                user.UserAuth == null ? null : user.UserAuth.GoogleEmail,
                user.UserAuth == null ? null : user.UserAuth.Phone, user.Status))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<RoleUserDto>>.Ok(users);
    }

    public async Task<Result<bool>> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        var user = await _dbContext.Users.IgnoreQueryFilters()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (role is null || user is null) return Result<bool>.NotFound("Role or user not found.");
        if (role.RoleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("Super Admin cannot be assigned through role management.");
        if (user.Role.RoleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("The Super Admin account role cannot be changed.");
        if (user.RoleId == roleId) return Result<bool>.Conflict("User already has this role.");

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await RevokeTokensAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_profileCache is not null) await _profileCache.RemoveAsync(userId, cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemoveRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);
        var defaultRole = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleName == UserRole.User, cancellationToken);
        var user = await _dbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (userRole is null || defaultRole is null || user is null || user.RoleId != roleId)
            return Result<bool>.NotFound("Role assignment not found.");
        if (userRole.RoleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("Super Admin role cannot be removed here.");
        if (userRole.RoleName == UserRole.User)
            return Result<bool>.Conflict("The default user role cannot be removed.");

        user.RoleId = defaultRole.RoleId;
        user.UpdatedAt = DateTime.UtcNow;
        await RevokeTokensAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_profileCache is not null) await _profileCache.RemoveAsync(userId, cancellationToken);
        return Result<bool>.Ok(true);
    }

    private async Task RevokeTokensAsync(uint userId, CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
    }

    private static string? Validate(SaveRoleRequest request)
    {
        var name = NormalizeName(request.RoleName ?? string.Empty);
        if (name.Length is < 2 or > 30 || !RoleNamePattern().IsMatch(name))
            return "Role name must be 2-30 characters and contain only lowercase letters, numbers, and underscores.";
        return null;
    }

    private static string NormalizeName(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_');
    private static RoleDto Map(Role role) => new(role.RoleId, role.RoleName);

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex RoleNamePattern();
}
