using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Services;

public interface IPersonalTopicQueryService
{
    Task<ListResult<IReadOnlyCollection<PersonalTopic>>> GetTopicsAsync(
        uint userId,
        PersonalTopicQuery query,
        CancellationToken cancellationToken = default);

    Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint topicId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default);
}
