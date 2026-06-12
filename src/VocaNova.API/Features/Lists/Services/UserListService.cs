using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;

namespace VocaNova.API.Features.Lists.Services;

public sealed class UserListService : IUserListService
{
    private readonly IUserListRepository _userListRepository;
    private readonly IUserListCache? _userListCache;

    public UserListService(
        IUserListRepository userListRepository,
        IUserListCache? userListCache = null)
    {
        _userListRepository = userListRepository;
        _userListCache = userListCache;
    }

    public async Task<Result<IReadOnlyCollection<UserListDto>>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<UserListDto>>.Unauthorized("Unauthorized.");
        }

        if (_userListCache is not null)
        {
            var cached = await _userListCache.GetAsync(userId, cancellationToken);
            if (cached is not null)
            {
                return Result<IReadOnlyCollection<UserListDto>>.Ok(cached);
            }
        }

        var lists = await _userListRepository.GetByUserAsync(userId, cancellationToken);

        if (_userListCache is not null)
        {
            await _userListCache.SetAsync(userId, lists, cancellationToken);
        }

        return Result<IReadOnlyCollection<UserListDto>>.Ok(lists);
    }

    public async Task<Result<UserListDto>> CreateAsync(
        uint userId,
        CreateListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<UserListDto>.Unauthorized("Unauthorized.");
        }

        var listName = request.ListName!.Trim();
        var normalizedListName = listName.ToLowerInvariant();

        var activeListCount = await _userListRepository.CountActiveAsync(userId, cancellationToken);
        if (activeListCount >= AppSettings.MaxListsPerUser)
        {
            return Result<UserListDto>.Fail($"A user can create at most {AppSettings.MaxListsPerUser} lists.");
        }

        if (await _userListRepository.ListNameExistsAsync(
            userId,
            normalizedListName,
            cancellationToken: cancellationToken))
        {
            return Result<UserListDto>.Conflict("List name already exists.");
        }

        var list = await _userListRepository.CreateAsync(userId, listName, cancellationToken);

        if (_userListCache is not null)
        {
            await _userListCache.RemoveAsync(userId, cancellationToken);
        }

        return Result<UserListDto>.Ok(list);
    }

    public async Task<Result<UserListDto>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<UserListDto>.Unauthorized("Unauthorized.");
        }

        var ownershipResult = await ValidateListOwnershipAsync(userId, listId, cancellationToken);
        if (!ownershipResult.IsSuccess)
        {
            return ToUserListFailure(ownershipResult);
        }

        var listName = request.ListName!.Trim();
        var normalizedListName = listName.ToLowerInvariant();
        if (await _userListRepository.ListNameExistsAsync(userId, normalizedListName, listId, cancellationToken))
        {
            return Result<UserListDto>.Conflict("List name already exists.");
        }

        var list = await _userListRepository.UpdateAsync(listId, listName, cancellationToken);
        if (list is null)
        {
            return Result<UserListDto>.NotFound("List not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);

        return Result<UserListDto>.Ok(list);
    }

    public async Task<Result<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<bool>.Unauthorized("Unauthorized.");
        }

        var ownershipResult = await ValidateListOwnershipAsync(userId, listId, cancellationToken);
        if (!ownershipResult.IsSuccess)
        {
            return ownershipResult;
        }

        var deleted = await _userListRepository.SoftDeleteWithWordsAsync(listId, cancellationToken);
        if (!deleted)
        {
            return Result<bool>.NotFound("List not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);

        return Result<bool>.Ok(true);
    }

    private async Task<Result<bool>> ValidateListOwnershipAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken)
    {
        if (listId == 0)
        {
            return Result<bool>.NotFound("List not found.");
        }

        var ownership = await _userListRepository.FindOwnershipAsync(listId, cancellationToken);
        if (ownership is null || ownership.Status == UserStatus.Deleted)
        {
            return Result<bool>.NotFound("List not found.");
        }

        if (ownership.UserId != userId)
        {
            return Result<bool>.Forbidden("You do not have access to this list.");
        }

        return Result<bool>.Ok(true);
    }

    private async Task RemoveCachedListsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_userListCache is not null)
        {
            await _userListCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static Result<UserListDto> ToUserListFailure(Result<bool> result)
    {
        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Result<UserListDto>.Unauthorized(result.Error!),
            StatusCodes.Status403Forbidden => Result<UserListDto>.Forbidden(result.Error!),
            StatusCodes.Status404NotFound => Result<UserListDto>.NotFound(result.Error!),
            _ => Result<UserListDto>.Fail(result.Error!),
        };
    }
}
