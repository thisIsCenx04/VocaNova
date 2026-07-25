using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.SuperAdmin.DTOs;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.SuperAdmin.Services;

public sealed class AdminUserAssignmentService
{
    private static readonly SemaphoreSlim AssignmentGate = new(1, 1);
    private readonly VocaNovaDbContext _dbContext;
    private readonly IAdminUserAssignmentStore _store;

    public AdminUserAssignmentService(VocaNovaDbContext dbContext, IAdminUserAssignmentStore store)
    {
        _dbContext = dbContext;
        _store = store;
    }

    public async Task<Result<AdminUserAssignmentOverviewDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _store.GetAllAsync(cancellationToken);
        var adminByUser = assignments
            .SelectMany(item => item.Value.Select(userId => new { AdminId = item.Key, UserId = userId }))
            .ToDictionary(item => item.UserId, item => item.AdminId);

        var admins = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Role.RoleName == UserRole.Admin && user.Status != UserStatus.Deleted)
            .OrderBy(user => user.UserProfile!.FullName)
            .Select(user => new AssignmentAdminDto(
                user.UserId,
                user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                user.UserAuth == null ? null : user.UserAuth.GoogleEmail))
            .ToListAsync(cancellationToken);

        var userRows = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Role.RoleName == UserRole.User && user.Status != UserStatus.Deleted)
            .OrderBy(user => user.UserProfile!.FullName)
            .Select(user => new
            {
                user.UserId,
                DisplayName = user.UserProfile == null ? string.Empty : user.UserProfile.FullName,
                Email = user.UserAuth == null ? null : user.UserAuth.GoogleEmail,
            })
            .ToListAsync(cancellationToken);
        var users = userRows.Select(user => new AssignmentUserDto(
            user.UserId, user.DisplayName, user.Email,
            adminByUser.TryGetValue(user.UserId, out var adminId) ? adminId : null)).ToArray();

        return Result<AdminUserAssignmentOverviewDto>.Ok(new(admins, users));
    }

    public async Task<Result<bool>> ReplaceAsync(
        uint adminId,
        IReadOnlyCollection<uint>? userIds,
        CancellationToken cancellationToken = default)
    {
        var selected = userIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
        var validAdmin = await _dbContext.Users.AnyAsync(user =>
            user.UserId == adminId && user.Role.RoleName == UserRole.Admin && user.Status != UserStatus.Deleted,
            cancellationToken);
        if (!validAdmin) return Result<bool>.NotFound("Admin account not found.");

        var validCount = await _dbContext.Users.CountAsync(user =>
            selected.Contains(user.UserId)
            && user.Role.RoleName == UserRole.User
            && user.Status != UserStatus.Deleted, cancellationToken);
        if (validCount != selected.Length)
            return Result<bool>.Fail("Every selected account must have the user role.");

        await AssignmentGate.WaitAsync(cancellationToken);
        try
        {
            var assignments = await _store.GetAllAsync(cancellationToken);
            var conflictingUserIds = assignments
                .Where(item => item.Key != adminId)
                .SelectMany(item => item.Value)
                .Intersect(selected)
                .Order()
                .ToArray();
            if (conflictingUserIds.Length > 0)
                return Result<bool>.Conflict(
                    "One or more selected users are already assigned to another administrator.");

            await _store.ReplaceAsync(adminId, selected, cancellationToken);
            return Result<bool>.Ok(true);
        }
        finally
        {
            AssignmentGate.Release();
        }
    }
}
