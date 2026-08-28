using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface ITopicReadRepository
{
    Task<IReadOnlyCollection<TopicSummary>> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        uint topicId,
        CancellationToken cancellationToken = default);
}
