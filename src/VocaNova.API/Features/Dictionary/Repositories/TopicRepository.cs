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

    public async Task<TopicSummaryDto> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var topic = new Topic
        {
            TopicName = request.TopicName!.Trim(),
            TopicNameVi = NormalizeNullable(request.TopicNameVi),
            Icon = NormalizeNullable(request.Icon),
            Status = UserStatus.Active,
        };

        _dbContext.Topics.Add(topic);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTopic(topic, wordCount: 0);
    }

    public async Task<TopicSummaryDto?> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var topic = await _dbContext.Topics
            .SingleOrDefaultAsync(entity => entity.TopicId == topicId, cancellationToken);
        if (topic is null)
        {
            return null;
        }

        topic.TopicName = request.TopicName!.Trim();
        topic.TopicNameVi = NormalizeNullable(request.TopicNameVi);
        topic.Icon = NormalizeNullable(request.Icon);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var wordCount = await _dbContext.WordTopics.CountAsync(
            wordTopic => wordTopic.TopicId == topic.TopicId,
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
}
