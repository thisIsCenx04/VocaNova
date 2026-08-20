using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface ITopicCache
{
    Task<IReadOnlyCollection<TopicSummary>?> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    Task SetTopicsAsync(
        IReadOnlyCollection<TopicSummary> topics,
        CancellationToken cancellationToken = default);

    Task<PagedCollection<WordSummary>?> GetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task SetTopicWordsAsync(
        uint topicId,
        int page,
        int limit,
        PagedCollection<WordSummary> words,
        CancellationToken cancellationToken = default);

    Task RemoveTopicsAsync(CancellationToken cancellationToken = default);

    Task RemoveTopicWordsAsync(uint topicId, CancellationToken cancellationToken = default);
}
