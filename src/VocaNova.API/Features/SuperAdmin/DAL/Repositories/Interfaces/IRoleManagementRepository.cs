using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Models;

namespace VocaNova.API.Features.SuperAdmin.BLL.Abstractions;

public interface IRoleManagementRepository
{
    Task<PagedResult<RoleModel>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsExcludeAsync(uint roleId, string roleName, CancellationToken cancellationToken = default);
    Task<RoleModel> AddRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<RoleModel?> UpdateRoleAsync(uint roleId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(uint roleId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyCollection<RoleUserModel>?> GetRoleUsersAsync(uint roleId, CancellationToken cancellationToken = default);
    
    Task<bool> RoleIdExistsAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<string?> GetRoleNameAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<string?> GetUserRoleNameAsync(uint userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(uint roleId, uint userId, uint defaultRoleId, CancellationToken cancellationToken = default);
    
    Task<uint?> GetRoleIdByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> HasUsersWithRoleAsync(uint roleId, CancellationToken cancellationToken = default);
    Task RevokeActiveRefreshTokensAsync(uint userId, DateTime now, CancellationToken cancellationToken = default);
}
