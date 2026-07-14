using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface ITopicCache
{
    Task<IReadOnlyCollection<TopicSummaryDto>?> GetTopicsAsync(CancellationToken cancellationToken = default);

    Task SetTopicsAsync(
        IReadOnlyCollection<TopicSummaryDto> topics,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WordSummaryDto>?> GetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task SetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        PagedResult<WordSummaryDto> words,
        CancellationToken cancellationToken = default);

    Task RemoveTopicsAsync(CancellationToken cancellationToken = default);

    Task RemoveTopicWordsAsync(uint topicId, CancellationToken cancellationToken = default);
}
