using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;

namespace VocaNova.API.Features.Admin.Services;

public interface IAdminUserService
{
    Task<Result<PagedResult<AdminUserSummaryDto>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserDetailDto>> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeactivateAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RestoreAsync(
        uint userId,
        CancellationToken cancellationToken = default);
}
