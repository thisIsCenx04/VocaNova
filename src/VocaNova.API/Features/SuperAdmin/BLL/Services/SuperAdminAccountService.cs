using VocaNova.API.Common.Abstractions.Transactions;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Abstractions;
using VocaNova.API.Features.SuperAdmin.BLL.Models;
using VocaNova.API.Features.SuperAdmin.BLL.Services.IServices;

namespace VocaNova.API.Features.SuperAdmin.BLL.Services;

public sealed class SuperAdminAccountService : ISuperAdminAccountService
{
    private readonly ISuperAdminAccountRepository _repository;
    private readonly IApplicationTransactionManager _transactionManager;
    private readonly IUserProfileCache? _userProfileCache;

    public SuperAdminAccountService(
        ISuperAdminAccountRepository repository,
        IApplicationTransactionManager transactionManager,
        IUserProfileCache? userProfileCache = null)
    {
        _repository = repository;
        _transactionManager = transactionManager;
        _userProfileCache = userProfileCache;
    }

    public async Task<Result<PagedResult<AdminAccountModel>>> GetAccountsAsync(
        AdminAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminAccountModel>>.Fail("Paging parameters are invalid.");
        }

        var status = Normalize(query.Status);
        if (status is not null && !UserStatus.All.Contains(status))
        {
            return Result<PagedResult<AdminAccountModel>>.Fail("Status is invalid.");
        }

        var result = await _repository.GetAccountsAsync(query, cancellationToken);
        return Result<PagedResult<AdminAccountModel>>.Ok(result);
    }

    public async Task<Result<AdminAccountModel>> GetAccountAsync(
        uint adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await _repository.GetAccountAsync(adminId, cancellationToken);
        return admin is null
            ? Result<AdminAccountModel>.NotFound("Admin account not found.")
            : Result<AdminAccountModel>.Ok(admin);
    }

    public async Task<Result<AdminAccountModel>> CreateAsync(
        CreateAdminAccountModel request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        var duplicate = await _repository.FindDuplicateAsync(phone, email, null, cancellationToken);
        if (duplicate is not null)
        {
            return Result<AdminAccountModel>.Conflict(duplicate);
        }

        var roleId = await _repository.GetAdminRoleIdAsync(cancellationToken);
        if (roleId is null)
        {
            return Result<AdminAccountModel>.Fail("Admin role is not configured.");
        }

        var passwordHash = PasswordHelper.Hash(request.Password!);
        
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var admin = await _repository.AddAccountAsync(request, passwordHash, roleId.Value, cancellationToken);
            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<AdminAccountModel>.Ok(admin);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<AdminAccountModel>> UpdateAsync(
        uint adminId,
        UpdateAdminAccountModel request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        
        var duplicate = await _repository.FindDuplicateAsync(phone, email, adminId, cancellationToken);
        if (duplicate is not null)
        {
            return Result<AdminAccountModel>.Conflict(duplicate);
        }

        var currentStatus = await _repository.GetAccountStatusAsync(adminId, cancellationToken);
        if (currentStatus == UserStatus.Deleted)
        {
            return Result<AdminAccountModel>.NotFound("Admin account not found.");
        }

        var passwordHash = string.IsNullOrWhiteSpace(request.Password) ? null : PasswordHelper.Hash(request.Password);
        
        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var newStatus = Normalize(request.Status) ?? currentStatus;
            var now = DateTime.UtcNow;

            if (passwordHash != null)
            {
                await _repository.RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
            }
            
            if (newStatus == UserStatus.Locked && currentStatus != UserStatus.Locked)
            {
                await _repository.RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
            }

            var admin = await _repository.UpdateAccountAsync(adminId, request, passwordHash, cancellationToken);
            if (admin is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AdminAccountModel>.NotFound("Admin account not found.");
            }

            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            await RemoveCachedProfileAsync(adminId, cancellationToken);
            return Result<AdminAccountModel>.Ok(admin);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<Result<bool>> LockAsync(uint adminId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(adminId, UserStatus.Locked, cancellationToken);

    public Task<Result<bool>> UnlockAsync(uint adminId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(adminId, UserStatus.Active, cancellationToken);

    public async Task<Result<bool>> DeleteAsync(uint adminId, CancellationToken cancellationToken = default)
    {
        var currentStatus = await _repository.GetAccountStatusAsync(adminId, cancellationToken);
        if (currentStatus is null || currentStatus == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("Admin account not found.");
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            await _repository.RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
            var success = await _repository.DeleteAccountAsync(adminId, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.NotFound("Admin account not found.");
            }

            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            await RemoveCachedProfileAsync(adminId, cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Result<bool>> ChangeStatusAsync(
        uint adminId,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var currentStatus = await _repository.GetAccountStatusAsync(adminId, cancellationToken);
        if (currentStatus is null || currentStatus == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("Admin account not found.");
        }

        if (currentStatus == newStatus)
        {
            return Result<bool>.Conflict($"Admin account is already {newStatus}.");
        }

        await using var transaction = await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            if (newStatus == UserStatus.Locked)
            {
                await _repository.RevokeActiveRefreshTokensAsync(adminId, now, cancellationToken);
            }

            var success = await _repository.ChangeStatusAsync(adminId, newStatus, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.NotFound("Admin account not found.");
            }

            await transaction.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            await RemoveCachedProfileAsync(adminId, cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private Task RemoveCachedProfileAsync(uint adminId, CancellationToken cancellationToken) =>
        _userProfileCache?.RemoveAsync(adminId, cancellationToken) ?? Task.CompletedTask;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
