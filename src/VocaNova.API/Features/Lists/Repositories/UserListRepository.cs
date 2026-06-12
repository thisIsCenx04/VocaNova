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
        uint? excludingListId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserLists
            .IgnoreQueryFilters()
            .AnyAsync(
                list => list.UserId == userId
                    && list.Status == UserStatus.Active
                    && list.ListName.ToLower() == normalizedListName
                    && (!excludingListId.HasValue || list.ListId != excludingListId.Value),
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

    public async Task<UserListOwnershipDto?> FindOwnershipAsync(
        uint listId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserLists
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(list => list.ListId == listId)
            .Select(list => new UserListOwnershipDto(list.ListId, list.UserId, list.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserListDto?> UpdateAsync(
        uint listId,
        string listName,
        CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.UserLists
            .SingleOrDefaultAsync(entity => entity.ListId == listId, cancellationToken);
        if (list is null)
        {
            return null;
        }

        list.ListName = listName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var wordCount = await _dbContext.UserListWords.CountAsync(
            listWord => listWord.ListId == list.ListId,
            cancellationToken);

        return new UserListDto(
            list.ListId,
            list.ListName,
            wordCount,
            list.CreatedAt);
    }

    public async Task<bool> SoftDeleteWithWordsAsync(
        uint listId,
        CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.UserLists
            .SingleOrDefaultAsync(entity => entity.ListId == listId, cancellationToken);
        if (list is null)
        {
            return false;
        }

        list.Status = UserStatus.Deleted;

        var listWords = await _dbContext.UserListWords
            .IgnoreQueryFilters()
            .Where(listWord => listWord.ListId == listId)
            .ToListAsync(cancellationToken);

        foreach (var listWord in listWords)
        {
            listWord.Status = UserStatus.Deleted;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
