using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public interface ITopicReadService
{
    Task<DictionaryResult<IReadOnlyCollection<TopicSummary>>> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    Task<DictionaryResult<PagedCollection<WordSummary>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default);
}
