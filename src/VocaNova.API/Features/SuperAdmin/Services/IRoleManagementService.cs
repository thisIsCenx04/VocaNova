using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.DTOs;

namespace VocaNova.API.Features.SuperAdmin.Services;

public interface IRoleManagementService
{
    Task<Result<PagedResult<RoleDto>>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> UpdateAsync(uint roleId, SaveRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<RoleUserDto>>> GetUsersAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<Result<bool>> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default);
}
