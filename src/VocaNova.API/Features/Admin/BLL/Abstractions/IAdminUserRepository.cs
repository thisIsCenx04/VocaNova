using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.Repositories;

public interface IAdminUserRepository
{
    Task<PagedResult<AdminUserSummaryModel>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailModel?> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<AdminUserTopicsModel> GetUserTopicsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminUserTestSessionModel>> GetTestHistoryAsync(
        uint userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<User?> FindUserForStatusUpdateAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<int> RevokeActiveRefreshTokensAsync(
        uint userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
