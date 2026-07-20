using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.DTOs;

namespace VocaNova.API.Features.SuperAdmin.Services;

public interface ISuperAdminAccountService
{
    Task<Result<PagedResult<AdminAccountDto>>> GetAccountsAsync(AdminAccountQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountDto>> GetAccountAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountDto>> CreateAsync(CreateAdminAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountDto>> UpdateAsync(uint adminId, UpdateAdminAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> LockAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UnlockAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(uint adminId, CancellationToken cancellationToken = default);
}
