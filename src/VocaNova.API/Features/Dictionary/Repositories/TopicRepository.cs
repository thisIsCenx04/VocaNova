using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Dictionary.Repositories;

public sealed class TopicRepository : ITopicRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public TopicRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TopicSummaryDto>> GetTopicsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(topic => topic.TopicName)
            .Select(topic => new TopicSummaryDto(
                topic.TopicId,
                topic.TopicName,
                topic.TopicNameVi,
                topic.Icon,
                topic.WordTopics.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AdminTopicDto>> GetAdminTopicsAsync(
        AdminTopicQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = query.IncludeDeleted
            ? _dbContext.Topics.IgnoreQueryFilters().AsNoTracking()
            : _dbContext.Topics.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(topic => topic.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            source = source.Where(topic =>
                topic.TopicName.ToLower().Contains(term)
                || (topic.TopicNameVi != null && topic.TopicNameVi.ToLower().Contains(term)));
        }

        return await source
            .OrderBy(topic => topic.TopicName)
            .Select(topic => new AdminTopicDto(
                topic.TopicId,
                topic.TopicName,
                topic.TopicNameVi,
                topic.Icon,
                topic.Status,
                // word_count = số từ active dùng topic (khớp guard xóa HasActiveWordsAsync).
                topic.WordTopics.Count(wordTopic => wordTopic.Word.Status == UserStatus.Active)))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(uint topicId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Topics.AnyAsync(topic => topic.TopicId == topicId, cancellationToken);
    }

    public Task<bool> TopicNameExistsAsync(
        string topicName,
        uint? excludingTopicId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Topics
            .IgnoreQueryFilters()
            .AnyAsync(
                topic => topic.TopicName == topicName
                    && (!excludingTopicId.HasValue || topic.TopicId != excludingTopicId.Value),
                cancellationToken);
    }

    public Task<bool> ActiveTopicNameExistsAsync(
        string topicName,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Topics.AnyAsync(topic => topic.TopicName == topicName, cancellationToken);
    }

    public Task<bool> ActiveTopicNameViExistsAsync(
        string topicNameVi,
        uint? excludingTopicId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = topicNameVi.Trim().ToLower();
        return _dbContext.Topics.AnyAsync(
            topic => topic.TopicNameVi != null && topic.TopicNameVi.ToLower() == normalizedName
                && (!excludingTopicId.HasValue || topic.TopicId != excludingTopicId.Value),
            cancellationToken);
    }

    public async Task<bool> WordIdsExistAsync(
        IReadOnlyCollection<uint> wordIds,
        CancellationToken cancellationToken = default)
    {
        if (wordIds.Count == 0) return true;

        var count = await _dbContext.Words.IgnoreQueryFilters().CountAsync(
            word => wordIds.Contains(word.WordId),
            cancellationToken);
        return count == wordIds.Distinct().Count();
    }

    public async Task<int> AddWordsAsync(
        uint topicId,
        IReadOnlyCollection<uint> wordIds,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await _dbContext.WordTopics
            .Where(link => link.TopicId == topicId && wordIds.Contains(link.WordId))
            .Select(link => link.WordId)
            .ToListAsync(cancellationToken);
        var newIds = wordIds.Distinct().Except(existingIds).ToArray();

        _dbContext.WordTopics.AddRange(newIds.Select(wordId => new WordTopic
        {
            TopicId = topicId,
            WordId = wordId,
            IsPrimary = true,
        }));
        await RestoreWordsAsync(newIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var persistedIds = await _dbContext.WordTopics.AsNoTracking()
            .Where(link => link.TopicId == topicId && newIds.Contains(link.WordId))
            .Select(link => link.WordId)
            .ToArrayAsync(cancellationToken);
        return persistedIds.Length;
    }

    public async Task<TopicSummaryDto> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var topicName = request.TopicName!.Trim();
        var topic = await _dbContext.Topics
            .IgnoreQueryFilters()
            .Include(entity => entity.WordTopics)
            .SingleOrDefaultAsync(
                entity => entity.TopicName == topicName && entity.Status == UserStatus.Deleted,
                cancellationToken);

        if (topic is null)
        {
            topic = new Topic { TopicName = topicName };
            _dbContext.Topics.Add(topic);
        }
        else
        {
            _dbContext.WordTopics.RemoveRange(topic.WordTopics);
            topic.WordTopics.Clear();
        }

        topic.TopicNameVi = NormalizeNullable(request.TopicNameVi);
        topic.Icon = NormalizeNullable(request.Icon);
        topic.Status = UserStatus.Active;

        await RestoreWordsAsync(request.WordIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var selectedWordIds = request.WordIds?.Distinct().ToArray() ?? Array.Empty<uint>();
        if (selectedWordIds.Length > 0)
        {
            _dbContext.WordTopics.AddRange(selectedWordIds.Select(wordId => new WordTopic
            {
                TopicId = topic.TopicId,
                WordId = wordId,
                IsPrimary = true,
            }));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureWordLinksPersistedAsync(topic.TopicId, selectedWordIds, cancellationToken);

        var wordCount = await _dbContext.WordTopics.CountAsync(
            link => link.TopicId == topic.TopicId && link.Word.Status == UserStatus.Active,
            cancellationToken);
        return MapTopic(topic, wordCount);
    }

    public async Task<TopicSummaryDto?> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics
            .Include(entity => entity.WordTopics)
            .SingleOrDefaultAsync(entity => entity.TopicId == topicId, cancellationToken);
        if (topic is null)
        {
            return null;
        }

        topic.TopicName = request.TopicName!.Trim();
        topic.TopicNameVi = NormalizeNullable(request.TopicNameVi);
        topic.Icon = NormalizeNullable(request.Icon);

        if (request.WordIds is not null)
        {
            _dbContext.WordTopics.RemoveRange(topic.WordTopics);
            topic.WordTopics.Clear();
            await RestoreWordsAsync(request.WordIds, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.WordIds is not null && request.WordIds.Count > 0)
        {
            _dbContext.WordTopics.AddRange(request.WordIds.Distinct().Select(wordId => new WordTopic
            {
                TopicId = topic.TopicId,
                WordId = wordId,
                IsPrimary = true,
            }));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (request.WordIds is not null)
        {
            await EnsureWordLinksPersistedAsync(topic.TopicId, request.WordIds, cancellationToken);
        }

        var wordCount = await _dbContext.WordTopics.CountAsync(
            wordTopic => wordTopic.TopicId == topic.TopicId && wordTopic.Word.Status == UserStatus.Active,
            cancellationToken);

        return MapTopic(topic, wordCount);
    }

    public Task<bool> HasActiveWordsAsync(uint topicId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WordTopics.AnyAsync(
            wordTopic => wordTopic.TopicId == topicId && wordTopic.Word.Status == UserStatus.Active,
            cancellationToken);
    }

    public async Task<bool> SetStatusAsync(
        uint topicId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.TopicId == topicId, cancellationToken);
        if (topic is null)
        {
            return false;
        }

        topic.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static TopicSummaryDto MapTopic(Topic topic, int wordCount)
    {
        return new TopicSummaryDto(
            topic.TopicId,
            topic.TopicName,
            topic.TopicNameVi,
            topic.Icon,
            wordCount);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task RestoreWordsAsync(
        IReadOnlyCollection<uint>? wordIds,
        CancellationToken cancellationToken)
    {
        if (wordIds is null || wordIds.Count == 0) return;

        var words = await _dbContext.Words.IgnoreQueryFilters()
            .Where(word => wordIds.Contains(word.WordId) && word.Status == UserStatus.Deleted)
            .ToListAsync(cancellationToken);
        foreach (var word in words) word.Status = UserStatus.Active;
    }

    private async Task EnsureWordLinksPersistedAsync(
        uint topicId,
        IReadOnlyCollection<uint> expectedWordIds,
        CancellationToken cancellationToken)
    {
        var expected = expectedWordIds.Distinct().OrderBy(id => id).ToArray();
        var actual = await _dbContext.WordTopics.AsNoTracking()
            .Where(link => link.TopicId == topicId)
            .OrderBy(link => link.WordId)
            .Select(link => link.WordId)
            .ToArrayAsync(cancellationToken);

        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                $"Vocabulary links for topic {topicId} were not persisted correctly.");
        }
    }
}
