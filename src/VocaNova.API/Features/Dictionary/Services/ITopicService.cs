using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Services;

public interface ITopicService
{
    Task<Result<IReadOnlyCollection<TopicSummaryDto>>> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<WordSummaryDto>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default);
}
