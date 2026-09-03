using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Models;

namespace VocaNova.API.Features.Admin.BLL.Services.IServices;

public interface IAdminUserService
{
    Task<Result<PagedResult<AdminUserSummaryModel>>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserDetailModel>> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AdminUserTestSessionModel>>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<Result<AdminUserTopicsModel>> GetUserTopicsAsync(
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
