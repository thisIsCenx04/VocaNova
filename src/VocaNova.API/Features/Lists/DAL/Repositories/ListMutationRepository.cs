using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Lists.DAL.Repositories;

public sealed class ListMutationRepository : IListMutationRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public ListMutationRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CountActiveAsync(
        uint userId,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserLists.CountAsync(
            list => list.UserId == userId
                && list.Status == UserStatus.Active
                && !list.ListName.StartsWith(PersonalTopicListName.Prefix),
            cancellationToken);

    public Task<bool> ListNameExistsAsync(
        uint userId,
        string normalizedListName,
        uint? excludingListId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.UserLists
            .IgnoreQueryFilters()
            .AnyAsync(
                list => list.UserId == userId
                    && list.Status == UserStatus.Active
                    && list.ListName.ToLower() == normalizedListName
                    && (!excludingListId.HasValue || list.ListId != excludingListId.Value),
                cancellationToken);

    public async Task<UserListSummary> CreateAsync(
        uint userId,
        CreateListCommand command,
        CancellationToken cancellationToken = default)
    {
        var list = new UserList
        {
            UserId = userId,
            ListName = command.ListName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        _dbContext.UserLists.Add(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ListMutationPersistenceMappings.ToUserListSummary(list, 0);
    }

    public async Task<ListLookupResult<ListOwnership>> GetOwnershipAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default)
    {
        if (listId == 0)
        {
            return ListLookupResult<ListOwnership>.ListNotFound();
        }

        var ownership = await _dbContext.UserLists
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(list => list.ListId == listId)
            .Select(ListMutationPersistenceMappings.ToListOwnership)
            .SingleOrDefaultAsync(cancellationToken);
        if (ownership is null
            || ownership.Status == UserStatus.Deleted
            || PersonalTopicListName.IsReserved(ownership.ListName))
        {
            return ListLookupResult<ListOwnership>.ListNotFound();
        }

        return ownership.UserId == userId
            ? ListLookupResult<ListOwnership>.Success(ownership)
            : ListLookupResult<ListOwnership>.ListForbidden();
    }

    public async Task<UserListSummary?> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListCommand command,
        CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.UserLists
            .SingleOrDefaultAsync(
                entity => entity.ListId == listId && entity.UserId == userId,
                cancellationToken);
        if (list is null)
        {
            return null;
        }

        list.ListName = command.ListName;
        await _dbContext.SaveChangesAsync(cancellationToken);
        var wordCount = await _dbContext.UserListWords.CountAsync(
            listWord => listWord.ListId == list.ListId
                && listWord.Word.Status == UserStatus.Active,
            cancellationToken);
        return ListMutationPersistenceMappings.ToUserListSummary(list, wordCount);
    }

    public async Task<bool> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.UserLists.SingleOrDefaultAsync(
            entity => entity.ListId == listId && entity.UserId == userId,
            cancellationToken);
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

    public Task<bool> ActiveWordExistsAsync(
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Words.AnyAsync(word => word.WordId == wordId, cancellationToken);

    public async Task<ListWordState?> FindListWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.UserListWords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.ListId == listId
                && listWord.WordId == wordId)
            .Select(ListMutationPersistenceMappings.ToListWordState)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ListWord> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default)
    {
        var listWord = new UserListWord
        {
            UserId = userId,
            ListId = listId,
            WordId = command.WordId,
            AddMethod = command.AddMethod,
            Note = command.Note,
            AddedAt = DateTime.UtcNow,
            Status = UserStatus.Active,
        };
        _dbContext.UserListWords.Add(listWord);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await FindListWordModelAsync(userId, listId, command.WordId, cancellationToken))!;
    }

    public async Task<ListWord?> RestoreWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                entity => entity.UserId == userId
                    && entity.ListId == listId
                    && entity.WordId == command.WordId,
                cancellationToken);
        if (listWord is null)
        {
            return null;
        }

        listWord.Status = UserStatus.Active;
        listWord.AddMethod = command.AddMethod;
        listWord.Note = command.Note;
        listWord.AddedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await FindListWordModelAsync(userId, listId, command.WordId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<uint>> GetRandomTopicWordIdsAsync(
        uint userId,
        uint listId,
        uint? topicId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .Where(word => !_dbContext.UserListWords
                .IgnoreQueryFilters()
                .Any(listWord => listWord.UserId == userId
                    && listWord.ListId == listId
                    && listWord.WordId == word.WordId
                    && listWord.Status == UserStatus.Active));
        if (topicId.HasValue)
        {
            query = query.Where(word => word.WordTopics.Any(link => link.TopicId == topicId.Value));
        }

        var candidates = await query
            .Select(word => word.WordId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return TakeRandom(candidates, count);
    }

    public async Task<IReadOnlyCollection<uint>> GetRandomRelationWordIdsAsync(
        uint userId,
        uint listId,
        string relationType,
        int count,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.WordRelations
            .AsNoTracking()
            .Where(relation => relation.RelationType == relationType
                && relation.IsQuizEligible == true
                && relation.RelatedWordId.HasValue
                && relation.RelatedWordNavigation != null
                && !_dbContext.UserListWords
                    .IgnoreQueryFilters()
                    .Any(listWord => listWord.UserId == userId
                        && listWord.ListId == listId
                        && listWord.WordId == relation.RelatedWordId!.Value
                        && listWord.Status == UserStatus.Active))
            .Select(relation => relation.RelatedWordId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        return TakeRandom(candidates, count);
    }

    public async Task<bool> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords.SingleOrDefaultAsync(
            entity => entity.UserId == userId
                && entity.ListId == listId
                && entity.WordId == wordId,
            cancellationToken);
        if (listWord is null)
        {
            return false;
        }

        listWord.Status = UserStatus.Deleted;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ListWord?> UpdateNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords.SingleOrDefaultAsync(
            entity => entity.UserId == userId
                && entity.ListId == listId
                && entity.WordId == wordId,
            cancellationToken);
        if (listWord is null)
        {
            return null;
        }

        listWord.Note = note;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await FindListWordModelAsync(userId, listId, wordId, cancellationToken);
    }

    private async Task<ListWord?> FindListWordModelAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken) =>
        await _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.ListId == listId
                && listWord.WordId == wordId)
            .Select(ListMutationPersistenceMappings.ToListWord(_dbContext, userId))
            .SingleOrDefaultAsync(cancellationToken);

    private static IReadOnlyCollection<uint> TakeRandom(IList<uint> candidates, int count)
    {
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
        }

        return candidates.Take(count).ToArray();
    }
}
