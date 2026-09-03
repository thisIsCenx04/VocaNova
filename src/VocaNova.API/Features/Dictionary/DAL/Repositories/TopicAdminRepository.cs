using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using PersistenceWordTopic = VocaNova.API.Infrastructure.Persistence.Entities.WordTopic;

namespace VocaNova.API.Features.Dictionary.DAL.Repositories;

public sealed class TopicAdminRepository : ITopicAdminRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public TopicAdminRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<AdminTopic>> ListAsync(
        AdminTopicQuery filter, CancellationToken cancellationToken = default)
    {
        var source = filter.IncludeDeleted
            ? _dbContext.Topics.IgnoreQueryFilters().AsNoTracking()
            : _dbContext.Topics.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Status)) source = source.Where(topic => topic.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var term = filter.Q.Trim().ToLower();
            source = source.Where(topic => topic.TopicName.ToLower().Contains(term)
                || (topic.TopicNameVi != null && topic.TopicNameVi.ToLower().Contains(term)));
        }
        return await source.OrderBy(topic => topic.TopicName)
            .Select(topic => new AdminTopic(topic.TopicId, topic.TopicName, topic.TopicNameVi,
                topic.Icon, topic.Status,
                topic.WordTopics.Count(link => link.Word.Status == UserStatus.Active)))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(uint topicId, bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        (includeDeleted ? _dbContext.Topics.IgnoreQueryFilters() : _dbContext.Topics)
            .AnyAsync(topic => topic.TopicId == topicId, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name, string? nameVi, uint? excludingId = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(name))
            return (excludingId.HasValue ? _dbContext.Topics.IgnoreQueryFilters() : _dbContext.Topics)
                .AnyAsync(topic => topic.TopicName == name
                    && (!excludingId.HasValue || topic.TopicId != excludingId), cancellationToken);
        var normalized = nameVi!.Trim().ToLower();
        return _dbContext.Topics.AnyAsync(topic => topic.TopicNameVi != null
            && topic.TopicNameVi.ToLower() == normalized
            && (!excludingId.HasValue || topic.TopicId != excludingId), cancellationToken);
    }

    public async Task<bool> WordIdsExistAsync(IReadOnlyCollection<uint> wordIds, CancellationToken cancellationToken = default)
    {
        if (wordIds.Count == 0) return true;
        var count = await _dbContext.Words.IgnoreQueryFilters()
            .CountAsync(word => wordIds.Contains(word.WordId), cancellationToken);
        return count == wordIds.Distinct().Count();
    }

    public async Task<int> AddWordsAsync(uint topicId, IReadOnlyCollection<uint> wordIds, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.WordTopics.Where(link => link.TopicId == topicId && wordIds.Contains(link.WordId))
            .Select(link => link.WordId).ToListAsync(cancellationToken);
        var newIds = wordIds.Distinct().Except(existing).ToArray();
        _dbContext.WordTopics.AddRange(newIds.Select(wordId => new PersistenceWordTopic { TopicId = topicId, WordId = wordId, IsPrimary = true }));
        await RestoreWordsAsync(newIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await _dbContext.WordTopics.AsNoTracking()
            .CountAsync(link => link.TopicId == topicId && newIds.Contains(link.WordId), cancellationToken);
    }

    public async Task<TopicSummary> CreateAsync(CreateTopicCommand command, CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics.IgnoreQueryFilters().Include(entity => entity.WordTopics)
            .SingleOrDefaultAsync(entity => entity.TopicName == command.TopicName && entity.Status == UserStatus.Deleted, cancellationToken);
        if (topic is null) { topic = new Topic { TopicName = command.TopicName }; _dbContext.Topics.Add(topic); }
        else { _dbContext.WordTopics.RemoveRange(topic.WordTopics); topic.WordTopics.Clear(); }
        topic.TopicNameVi = command.TopicNameVi; topic.Icon = command.Icon; topic.Status = UserStatus.Active;
        await RestoreWordsAsync(command.WordIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var ids = command.WordIds?.Distinct().ToArray() ?? [];
        if (ids.Length > 0)
        {
            _dbContext.WordTopics.AddRange(ids.Select(wordId => new PersistenceWordTopic { TopicId = topic.TopicId, WordId = wordId, IsPrimary = true }));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        await EnsureWordLinksPersistedAsync(topic.TopicId, ids, cancellationToken);
        var count = await _dbContext.WordTopics.CountAsync(link => link.TopicId == topic.TopicId && link.Word.Status == UserStatus.Active, cancellationToken);
        return DictionaryAdminPersistenceMappings.ToTopicSummary(topic, count);
    }

    public async Task<TopicSummary?> UpdateAsync(
        uint topicId, UpdateTopicCommand command, CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics.Include(entity => entity.WordTopics)
            .SingleOrDefaultAsync(entity => entity.TopicId == topicId, cancellationToken);
        if (topic is null) return null;
        topic.TopicName = command.TopicName; topic.TopicNameVi = command.TopicNameVi; topic.Icon = command.Icon;
        if (command.WordIds is not null)
        {
            _dbContext.WordTopics.RemoveRange(topic.WordTopics); topic.WordTopics.Clear();
            await RestoreWordsAsync(command.WordIds, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (command.WordIds is { Count: > 0 })
        {
            _dbContext.WordTopics.AddRange(command.WordIds.Distinct().Select(wordId =>
                new PersistenceWordTopic { TopicId = topic.TopicId, WordId = wordId, IsPrimary = true }));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        if (command.WordIds is not null) await EnsureWordLinksPersistedAsync(topic.TopicId, command.WordIds, cancellationToken);
        var count = await _dbContext.WordTopics.CountAsync(link => link.TopicId == topic.TopicId && link.Word.Status == UserStatus.Active, cancellationToken);
        return DictionaryAdminPersistenceMappings.ToTopicSummary(topic, count);
    }

    public Task<bool> HasActiveWordsAsync(uint topicId, CancellationToken cancellationToken = default) =>
        _dbContext.WordTopics.AnyAsync(link => link.TopicId == topicId && link.Word.Status == UserStatus.Active, cancellationToken);

    public async Task<bool> SetStatusAsync(uint topicId, string status, CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.TopicId == topicId, cancellationToken);
        if (topic is null) return false;
        topic.Status = status; await _dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    private async Task RestoreWordsAsync(IReadOnlyCollection<uint>? wordIds, CancellationToken cancellationToken)
    {
        if (wordIds is null || wordIds.Count == 0) return;
        var words = await _dbContext.Words.IgnoreQueryFilters()
            .Where(word => wordIds.Contains(word.WordId) && word.Status == UserStatus.Deleted).ToListAsync(cancellationToken);
        foreach (var word in words) word.Status = UserStatus.Active;
    }

    private async Task EnsureWordLinksPersistedAsync(uint topicId, IReadOnlyCollection<uint> ids, CancellationToken cancellationToken)
    {
        var expected = ids.Distinct().OrderBy(id => id).ToArray();
        var actual = await _dbContext.WordTopics.AsNoTracking().Where(link => link.TopicId == topicId)
            .OrderBy(link => link.WordId).Select(link => link.WordId).ToArrayAsync(cancellationToken);
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException($"Vocabulary links for topic {topicId} were not persisted correctly.");
    }
}
