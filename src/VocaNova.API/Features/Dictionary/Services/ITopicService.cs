using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Services;

public interface ITopicService
{
    Task<Result<IReadOnlyCollection<TopicSummaryDto>>> GetTopicsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<AdminTopicDto>>> GetAdminTopicsAsync(
        AdminTopicQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<WordSummaryDto>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<TopicSummaryDto>> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<int>> AddWordsAsync(
        uint topicId,
        AddTopicWordsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<TopicSummaryDto>> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> SoftDeleteAsync(
        uint topicId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RestoreAsync(
        uint topicId,
        CancellationToken cancellationToken = default);
}
