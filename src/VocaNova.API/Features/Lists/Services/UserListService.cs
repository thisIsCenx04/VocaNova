using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Infrastructure.Caching;

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

        if (await _userListRepository.ListNameExistsAsync(userId, normalizedListName, cancellationToken))
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
}
