using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Lists.DAL.Repositories;

public sealed class PersonalTopicQueryRepository : IPersonalTopicQueryRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public PersonalTopicQueryRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ListLookupResult<IReadOnlyCollection<PersonalTopic>>> GetTopicsAsync(
        uint userId,
        uint? containsWordId,
        CancellationToken cancellationToken = default)
    {
        if (containsWordId.HasValue
            && !await _dbContext.Words.AnyAsync(
                word => word.WordId == containsWordId.Value,
                cancellationToken))
        {
            return ListLookupResult<IReadOnlyCollection<PersonalTopic>>.WordNotFound();
        }

        var topics = await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(topic => topic.TopicName)
            .Select(topic => new
            {
                topic.TopicId,
                topic.TopicName,
                topic.TopicNameVi,
                topic.Icon,
            })
            .ToListAsync(cancellationToken);

        var internalLists = await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => list.UserId == userId
                && list.Status == UserStatus.Active
                && list.ListName.StartsWith(PersonalTopicListName.Prefix))
            .Select(list => new
            {
                list.ListId,
                list.ListName,
                WordCount = list.UserListWords.Count(listWord =>
                    listWord.Word.Status == UserStatus.Active),
                ContainsWord = containsWordId.HasValue
                    && list.UserListWords.Any(listWord =>
                        listWord.WordId == containsWordId.Value
                        && listWord.Word.Status == UserStatus.Active),
            })
            .ToListAsync(cancellationToken);

        var listsByTopicId = internalLists
            .Select(list => PersonalTopicListName.TryGetTopicId(list.ListName, out var topicId)
                ? new { TopicId = topicId, List = list }
                : null)
            .Where(item => item is not null)
            .GroupBy(item => item!.TopicId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item!.List.ListId).First()!.List);

        IReadOnlyCollection<PersonalTopic> result = topics
            .Select(topic =>
            {
                listsByTopicId.TryGetValue(topic.TopicId, out var list);
                return new PersonalTopic(
                    topic.TopicId,
                    list?.ListId,
                    topic.TopicName,
                    topic.TopicNameVi,
                    topic.Icon,
                    list?.WordCount ?? 0,
                    list?.ContainsWord ?? false);
            })
            .ToArray();

        return ListLookupResult<IReadOnlyCollection<PersonalTopic>>.Success(result);
    }

    public async Task<ListLookupResult<PagedCollection<ListWord>>> GetTopicWordsAsync(
        uint userId,
        uint topicId,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Topics.AnyAsync(
            topic => topic.TopicId == topicId,
            cancellationToken))
        {
            return ListLookupResult<PagedCollection<ListWord>>.TopicNotFound();
        }

        var internalName = PersonalTopicListName.For(topicId);
        var listId = await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => list.UserId == userId
                && list.ListName == internalName
                && list.Status == UserStatus.Active)
            .OrderBy(list => list.ListId)
            .Select(list => (uint?)list.ListId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!listId.HasValue)
        {
            return ListLookupResult<PagedCollection<ListWord>>.Success(
                new PagedCollection<ListWord>(Array.Empty<ListWord>(), page, limit, 0));
        }

        var query = _dbContext.UserListWords
            .AsNoTracking()
            .Where(listWord => listWord.UserId == userId && listWord.ListId == listId.Value)
            .OrderByDescending(listWord => listWord.AddedAt)
            .ThenByDescending(listWord => listWord.WordId)
            .Select(ListPersistenceMappings.ToListWord(_dbContext, userId));
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return ListLookupResult<PagedCollection<ListWord>>.Success(
            new PagedCollection<ListWord>(items, page, limit, totalItems));
    }
}
