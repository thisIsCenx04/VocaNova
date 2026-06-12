using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Lists.Repositories;

public sealed class UserListRepository : IUserListRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public UserListRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserListDto>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => list.UserId == userId && list.Status == UserStatus.Active)
            .OrderByDescending(list => list.CreatedAt)
            .ThenByDescending(list => list.ListId)
            .Select(list => new UserListDto(
                list.ListId,
                list.ListName,
                list.UserListWords.Count,
                list.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLists
            .CountAsync(
                list => list.UserId == userId && list.Status == UserStatus.Active,
                cancellationToken);
    }

    public Task<bool> ListNameExistsAsync(
        uint userId,
        string normalizedListName,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLists
            .IgnoreQueryFilters()
            .AnyAsync(
                list => list.UserId == userId
                    && list.Status == UserStatus.Active
                    && list.ListName.ToLower() == normalizedListName,
                cancellationToken);
    }

    public async Task<UserListDto> CreateAsync(
        uint userId,
        string listName,
        CancellationToken cancellationToken = default)
    {
        var list = new UserList
        {
            UserId = userId,
            ListName = listName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.UserLists.Add(list);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserListDto(
            list.ListId,
            list.ListName,
            WordCount: 0,
            list.CreatedAt);
    }
}
