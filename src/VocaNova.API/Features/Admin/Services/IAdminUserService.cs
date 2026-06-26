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

    Task<Result<PagedResult<AdminUserTestSessionDto>>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserTopicsDto>> GetUserTopicsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    // actorRole = vai trò người thực hiện. Admin chỉ khóa/mở user thường; chỉ super_admin
    // mới khóa/mở được admin. Không ai khóa/mở super_admin qua màn quản trị.
    Task<Result<bool>> DeactivateAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RestoreAsync(
        uint userId,
        string actorRole,
        CancellationToken cancellationToken = default);
}
