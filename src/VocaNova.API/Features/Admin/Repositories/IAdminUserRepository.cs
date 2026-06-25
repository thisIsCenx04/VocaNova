using VocaNova.API.Common.Results;
using VocaNova.API.Features.Admin.DTOs;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Admin.Repositories;

public interface IAdminUserRepository
{
    Task<PagedResult<AdminUserSummaryDto>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetUserDetailAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminUserTestSessionDto>> GetTestHistoryAsync(
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
