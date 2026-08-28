using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Models;

namespace VocaNova.API.Features.SuperAdmin.BLL.Services.IServices;

public interface ISuperAdminAccountService
{
    Task<Result<PagedResult<AdminAccountModel>>> GetAccountsAsync(AdminAccountQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountModel>> GetAccountAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountModel>> CreateAsync(CreateAdminAccountModel request, CancellationToken cancellationToken = default);
    Task<Result<AdminAccountModel>> UpdateAsync(uint adminId, UpdateAdminAccountModel request, CancellationToken cancellationToken = default);
    Task<Result<bool>> LockAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UnlockAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(uint adminId, CancellationToken cancellationToken = default);
}
