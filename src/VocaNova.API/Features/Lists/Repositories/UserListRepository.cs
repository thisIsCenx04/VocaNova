using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
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
            .Where(list => list.UserId == userId
                && list.Status == UserStatus.Active
                && !list.ListName.StartsWith(PersonalTopicListName.Prefix))
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
                list => list.UserId == userId
                    && list.Status == UserStatus.Active
                    && !list.ListName.StartsWith(PersonalTopicListName.Prefix),
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
            .Select(list => new UserListOwnershipDto(
                list.ListId,
                list.UserId,
                list.Status,
                list.ListName))
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

    public Task<PagedResult<ListWordDto>> GetWordsAsync(
        uint userId,
        uint listId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId && listWord.ListId == listId)
            .OrderByDescending(listWord => listWord.AddedAt)
            .ThenByDescending(listWord => listWord.WordId)
            .Select(listWord => new ListWordDto(
                listWord.WordId,
                listWord.Word.Word1,
                listWord.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                // Số đúng/sai lấy từ tiến độ học của từ (per user+word) — đây là
                // nơi mỗi lần nộp đáp án quiz cập nhật. Bảng user_list_word_stats
                // không có tiến trình ghi nào nên luôn rỗng.
                _dbContext.UserWordProgresses
                    .Where(progress => progress.UserId == userId
                        && progress.WordId == listWord.WordId)
                    .Select(progress => (int?)progress.CorrectCount)
                    .FirstOrDefault() ?? 0,
                _dbContext.UserWordProgresses
                    .Where(progress => progress.UserId == userId
                        && progress.WordId == listWord.WordId)
                    .Select(progress => (int?)progress.WrongCount)
                    .FirstOrDefault() ?? 0,
                listWord.Note,
                listWord.AddedAt))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public Task<bool> ActiveWordExistsAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Words.AnyAsync(word => word.WordId == wordId, cancellationToken);
    }

    public async Task<ListWordStateDto?> FindListWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserListWords
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.ListId == listId
                && listWord.WordId == wordId)
            .Select(listWord => new ListWordStateDto(
                listWord.UserId,
                listWord.ListId,
                listWord.WordId,
                listWord.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ListWordDto> AddWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        string addMethod,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var listWord = new UserListWord
        {
            UserId = userId,
            ListId = listId,
            WordId = wordId,
            AddMethod = addMethod,
            Note = note,
            AddedAt = DateTime.UtcNow,
            Status = UserStatus.Active,
        };

        _dbContext.UserListWords.Add(listWord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await FindListWordDtoAsync(userId, listId, wordId, cancellationToken))!;
    }

    public async Task<ListWordDto?> RestoreWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        string addMethod,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                entity => entity.UserId == userId
                    && entity.ListId == listId
                    && entity.WordId == wordId,
                cancellationToken);
        if (listWord is null)
        {
            return null;
        }

        listWord.Status = UserStatus.Active;
        listWord.AddMethod = addMethod;
        listWord.Note = note;
        listWord.AddedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await FindListWordDtoAsync(userId, listId, wordId, cancellationToken);
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
            query = query.Where(word => word.WordTopics.Any(wordTopic => wordTopic.TopicId == topicId.Value));
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

    public async Task<bool> SoftDeleteWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords
            .SingleOrDefaultAsync(
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

    public async Task<ListWordDto?> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var listWord = await _dbContext.UserListWords
            .SingleOrDefaultAsync(
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

        return await FindListWordDtoAsync(userId, listId, wordId, cancellationToken);
    }

    private async Task<ListWordDto?> FindListWordDtoAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId
                && listWord.ListId == listId
                && listWord.WordId == wordId)
            .Select(listWord => new ListWordDto(
                listWord.WordId,
                listWord.Word.Word1,
                listWord.Word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .ThenBy(sense => sense.SenseId)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                _dbContext.UserWordProgresses
                    .Where(progress => progress.UserId == userId
                        && progress.WordId == listWord.WordId)
                    .Select(progress => (int?)progress.CorrectCount)
                    .FirstOrDefault() ?? 0,
                _dbContext.UserWordProgresses
                    .Where(progress => progress.UserId == userId
                        && progress.WordId == listWord.WordId)
                    .Select(progress => (int?)progress.WrongCount)
                    .FirstOrDefault() ?? 0,
                listWord.Note,
                listWord.AddedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IReadOnlyCollection<uint> TakeRandom(IList<uint> candidates, int count)
    {
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
        }

        return candidates
            .Take(count)
            .ToArray();
    }
}
