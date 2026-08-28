using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Abstractions;

public interface IPersonalTopicQueryRepository
{
    Task<ListLookupResult<IReadOnlyCollection<PersonalTopic>>> GetTopicsAsync(
        uint userId,
        uint? containsWordId,
        CancellationToken cancellationToken = default);

    Task<ListLookupResult<PagedCollection<ListWord>>> GetTopicWordsAsync(
        uint userId,
        uint topicId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
