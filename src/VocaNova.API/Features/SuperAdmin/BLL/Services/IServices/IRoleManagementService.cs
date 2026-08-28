using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Models;

namespace VocaNova.API.Features.SuperAdmin.BLL.Services.IServices;

public interface IRoleManagementService
{
    Task<Result<PagedResult<RoleModel>>> GetRolesAsync(RoleQuery query, CancellationToken cancellationToken = default);
    Task<Result<RoleModel>> CreateAsync(SaveRoleModel request, CancellationToken cancellationToken = default);
    Task<Result<RoleModel>> UpdateAsync(uint roleId, SaveRoleModel request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<RoleUserModel>>> GetUsersAsync(uint roleId, CancellationToken cancellationToken = default);
    Task<Result<bool>> AssignRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveRoleAsync(uint roleId, uint userId, CancellationToken cancellationToken = default);
}
