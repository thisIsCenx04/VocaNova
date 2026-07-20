using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Repositories;

public interface ITopicRepository
{
    Task<IReadOnlyCollection<TopicSummaryDto>> GetTopicsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AdminTopicDto>> GetAdminTopicsAsync(
        AdminTopicQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(uint topicId, CancellationToken cancellationToken = default);

    Task<bool> TopicNameExistsAsync(
        string topicName,
        uint? excludingTopicId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveTopicNameExistsAsync(
        string topicName,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveTopicNameViExistsAsync(
        string topicNameVi,
        uint? excludingTopicId = null,
        CancellationToken cancellationToken = default);

    Task<bool> WordIdsExistAsync(
        IReadOnlyCollection<uint> wordIds,
        CancellationToken cancellationToken = default);

    Task<int> AddWordsAsync(
        uint topicId,
        IReadOnlyCollection<uint> wordIds,
        CancellationToken cancellationToken = default);

    Task<TopicSummaryDto> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<TopicSummaryDto?> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveWordsAsync(uint topicId, CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(
        uint topicId,
        string status,
        CancellationToken cancellationToken = default);
}
