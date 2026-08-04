using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Lists.Repositories;

public sealed class PersonalTopicRepository : IPersonalTopicRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public PersonalTopicRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PersonalTopicDto>> GetTopicsAsync(
        uint userId,
        uint? wordId = null,
        CancellationToken cancellationToken = default)
    {
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
                ContainsWord = wordId.HasValue
                    && list.UserListWords.Any(listWord =>
                        listWord.WordId == wordId.Value
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

        return topics
            .Select(topic =>
            {
                listsByTopicId.TryGetValue(topic.TopicId, out var list);
                return new PersonalTopicDto(
                    topic.TopicId,
                    list?.ListId,
                    topic.TopicName,
                    topic.TopicNameVi,
                    topic.Icon,
                    list?.WordCount ?? 0,
                    list?.ContainsWord ?? false);
            })
            .ToArray();
    }

    public Task<bool> TopicExistsAsync(
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Topics.AnyAsync(
            topic => topic.TopicId == topicId,
            cancellationToken);
    }

    public Task<bool> WordBelongsToTopicAsync(
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WordTopics.AnyAsync(
            link => link.TopicId == topicId && link.WordId == wordId,
            cancellationToken);
    }

    public async Task<uint?> FindActiveListIdAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        var internalName = PersonalTopicListName.For(topicId);
        return await _dbContext.UserLists
            .AsNoTracking()
            .Where(list => list.UserId == userId
                && list.ListName == internalName
                && list.Status == UserStatus.Active)
            .OrderBy(list => list.ListId)
            .Select(list => (uint?)list.ListId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<uint> GetOrCreateListIdAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default)
    {
        var internalName = PersonalTopicListName.For(topicId);
        var list = await _dbContext.UserLists
            .IgnoreQueryFilters()
            .Where(entity => entity.UserId == userId && entity.ListName == internalName)
            .OrderBy(entity => entity.ListId)
            .FirstOrDefaultAsync(cancellationToken);

        if (list is null)
        {
            list = new UserList
            {
                UserId = userId,
                ListName = internalName,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.UserLists.Add(list);
        }
        else if (list.Status != UserStatus.Active)
        {
            list.Status = UserStatus.Active;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return list.ListId;
    }
}
