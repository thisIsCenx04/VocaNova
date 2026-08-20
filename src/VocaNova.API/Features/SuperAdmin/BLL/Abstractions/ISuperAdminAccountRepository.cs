using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.BLL.Models;

namespace VocaNova.API.Features.SuperAdmin.BLL.Abstractions;

public interface ISuperAdminAccountRepository
{
    Task<PagedResult<AdminAccountModel>> GetAccountsAsync(AdminAccountQuery query, CancellationToken cancellationToken = default);
    Task<AdminAccountModel?> GetAccountAsync(uint adminId, CancellationToken cancellationToken = default);
    Task<string?> FindDuplicateAsync(string phone, string email, uint? excludedUserId, CancellationToken cancellationToken = default);
    Task<uint?> GetAdminRoleIdAsync(CancellationToken cancellationToken = default);
    
    Task<AdminAccountModel> AddAccountAsync(CreateAdminAccountModel request, string passwordHash, uint roleId, CancellationToken cancellationToken = default);
    Task<AdminAccountModel?> UpdateAccountAsync(uint adminId, UpdateAdminAccountModel request, string? passwordHash, CancellationToken cancellationToken = default);
    Task<bool> ChangeStatusAsync(uint adminId, string newStatus, CancellationToken cancellationToken = default);
    Task<bool> DeleteAccountAsync(uint adminId, CancellationToken cancellationToken = default);
    Task RevokeActiveRefreshTokensAsync(uint adminId, DateTime now, CancellationToken cancellationToken = default);
    Task<string?> GetAccountStatusAsync(uint adminId, CancellationToken cancellationToken = default);
}
