using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Infrastructure.Persistence;

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

    public Task<bool> ExistsAsync(uint topicId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Topics.AnyAsync(topic => topic.TopicId == topicId, cancellationToken);
    }
}
