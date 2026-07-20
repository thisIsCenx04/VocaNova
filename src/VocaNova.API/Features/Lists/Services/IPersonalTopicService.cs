using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Services;

public interface IPersonalTopicService
{
    Task<Result<IReadOnlyCollection<PersonalTopicDto>>> GetTopicsAsync(
        uint userId,
        uint? wordId = null,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<ListWordDto>>> GetWordsAsync(
        uint userId,
        uint topicId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PersonalTopicDto>> AddWordAsync(
        uint userId,
        uint topicId,
        AddPersonalTopicWordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveWordAsync(
        uint userId,
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
