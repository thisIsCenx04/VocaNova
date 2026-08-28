using System.Text.RegularExpressions;
using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Services.IServices;

namespace VocaNova.API.Features.SuperAdmin.BLL.Services;

public sealed partial class RoleManagementService : IRoleManagementService
{
    private readonly IRoleManagementRepository _repository;
    private readonly IApplicationTransactionManager _transactionManager;
    private readonly IUserProfileCache? _profileCache;

    public RoleManagementService(
        IRoleManagementRepository repository, 
        IApplicationTransactionManager transactionManager,
        IUserProfileCache? profileCache = null)
    {
        _repository = repository;
        _transactionManager = transactionManager;
        _profileCache = profileCache;
    }

    public async Task<Result<PagedResult<RoleModel>>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
            return Result<PagedResult<RoleModel>>.Fail("Paging parameters are invalid.");

        var type = query.Type?.Trim().ToLowerInvariant();
        if (type is not null and not "" && type != "system" && type != "custom")
            return Result<PagedResult<RoleModel>>.Fail("Role type is invalid.");

        var sortBy = query.SortBy?.Trim().ToLowerInvariant();
        if (sortBy is not null and not "" && sortBy is not ("id" or "name" or "type"))
            return Result<PagedResult<RoleModel>>.Fail("Sort column is invalid.");

        var sortDirection = query.SortDirection?.Trim().ToLowerInvariant();
        if (sortDirection is not null and not "" && sortDirection is not ("asc" or "desc"))
            return Result<PagedResult<RoleModel>>.Fail("Sort direction must be 'asc' or 'desc'.");

        var result = await _repository.GetRolesAsync(query, cancellationToken);
        return Result<PagedResult<RoleModel>>.Ok(result);
    }

    public async Task<Result<RoleModel>> CreateAsync(SaveRoleModel request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null) return Result<RoleModel>.Fail(validation);
        var name = NormalizeName(request.RoleName!);
        
        if (await _repository.RoleExistsAsync(name, cancellationToken))
            return Result<RoleModel>.Conflict("Role name already exists.");

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var role = await _repository.AddRoleAsync(name, cancellationToken);
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<RoleModel>.Ok(role);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<RoleModel>> UpdateAsync(uint roleId, SaveRoleModel request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null) return Result<RoleModel>.Fail(validation);
        
        var name = NormalizeName(request.RoleName!);
        var oldName = await _repository.GetRoleNameAsync(roleId, cancellationToken);
        
        if (oldName is null) return Result<RoleModel>.NotFound("Role not found.");
        
        if (UserRole.All.Contains(oldName) && !string.Equals(oldName, name, StringComparison.Ordinal))
            return Result<RoleModel>.Forbidden("System role names cannot be changed.");
            
        if (await _repository.RoleExistsExcludeAsync(roleId, name, cancellationToken))
            return Result<RoleModel>.Conflict("Role name already exists.");

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var role = await _repository.UpdateRoleAsync(roleId, name, cancellationToken);
            if (role is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<RoleModel>.NotFound("Role not found.");
            }
            
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<RoleModel>.Ok(role);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<bool>> DeleteAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        var roleName = await _repository.GetRoleNameAsync(roleId, cancellationToken);
        if (roleName is null) return Result<bool>.NotFound("Role not found.");
        
        if (UserRole.All.Contains(roleName))
            return Result<bool>.Forbidden("System roles cannot be deleted.");
            
        if (await _repository.HasUsersWithRoleAsync(roleId, cancellationToken))
            return Result<bool>.Conflict("Role is still assigned to one or more users.");
            
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var success = await _repository.DeleteRoleAsync(roleId, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.NotFound("Role not found.");
            }
            
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<IReadOnlyCollection<RoleUserModel>>> GetUsersAsync(uint roleId, CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetRoleUsersAsync(roleId, cancellationToken);
        if (users is null)
            return Result<IReadOnlyCollection<RoleUserModel>>.NotFound("Role not found.");
            
        return Result<IReadOnlyCollection<RoleUserModel>>.Ok(users);
    }

    public async Task<Result<bool>> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default)
    {
        var roleName = await _repository.GetRoleNameAsync(roleId, cancellationToken);
        var userRoleName = await _repository.GetUserRoleNameAsync(userId, cancellationToken);
        
        if (roleName is null || userRoleName is null) return Result<bool>.NotFound("Role or user not found.");
        
        if (roleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("Super Admin cannot be assigned through role management.");
            
        if (userRoleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("The Super Admin account role cannot be changed.");
            
        var currentRoleId = await _repository.GetRoleIdByNameAsync(userRoleName, cancellationToken);
        if (currentRoleId == roleId) return Result<bool>.Conflict("User already has this role.");

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var success = await _repository.AssignRoleAsync(roleId, userId, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.NotFound("Role or user not found.");
            }
            
            await _repository.RevokeActiveRefreshTokensAsync(userId, DateTime.UtcNow, cancellationToken);
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            if (_profileCache is not null) await _profileCache.RemoveAsync(userId, cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<bool>> RemoveRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default)
    {
        var roleName = await _repository.GetRoleNameAsync(roleId, cancellationToken);
        var defaultRoleId = await _repository.GetRoleIdByNameAsync(UserRole.User, cancellationToken);
        var userRoleName = await _repository.GetUserRoleNameAsync(userId, cancellationToken);
        
        if (roleName is null || defaultRoleId is null || userRoleName is null)
            return Result<bool>.NotFound("Role assignment not found.");
            
        var currentRoleId = await _repository.GetRoleIdByNameAsync(userRoleName, cancellationToken);
        if (currentRoleId != roleId)
            return Result<bool>.NotFound("Role assignment not found.");
            
        if (roleName == UserRole.SuperAdmin)
            return Result<bool>.Forbidden("Super Admin role cannot be removed here.");
            
        if (roleName == UserRole.User)
            return Result<bool>.Conflict("The default user role cannot be removed.");

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var success = await _repository.RemoveRoleAsync(roleId, userId, defaultRoleId.Value, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.NotFound("Role assignment not found.");
            }
            
            await _repository.RevokeActiveRefreshTokensAsync(userId, DateTime.UtcNow, cancellationToken);
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            if (_profileCache is not null) await _profileCache.RemoveAsync(userId, cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string? Validate(SaveRoleModel request)
    {
        var name = NormalizeName(request.RoleName ?? string.Empty);
        if (name.Length is < 2 or > 30 || !RoleNamePattern().IsMatch(name))
            return "Role name must be 2-30 characters and contain only lowercase letters, numbers, and underscores.";
        return null;
    }

    private static string NormalizeName(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_');

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex RoleNamePattern();
}
