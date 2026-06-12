using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Repositories;

public interface ITopicRepository
{
    Task<IReadOnlyCollection<TopicSummaryDto>> GetTopicsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(uint topicId, CancellationToken cancellationToken = default);
}
