using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Dictionary.DAL.Repositories;

public sealed class TopicReadRepository : ITopicReadRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public TopicReadRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TopicSummary>> GetTopicsAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext.Topics
            .AsNoTracking()
            .OrderBy(topic => topic.TopicName)
            .Select(DictionaryPersistenceMappings.ToTopicSummary)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        uint topicId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Topics.AnyAsync(topic => topic.TopicId == topicId, cancellationToken);
}
