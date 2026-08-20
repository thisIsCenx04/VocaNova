using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface ITopicAdminRepository
{
    Task<IReadOnlyCollection<AdminTopic>> ListAsync(AdminTopicQuery query, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(uint topicId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, string? nameVi, uint? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> WordIdsExistAsync(IReadOnlyCollection<uint> wordIds, CancellationToken cancellationToken = default);
    Task<TopicSummary> CreateAsync(CreateTopicCommand command, CancellationToken cancellationToken = default);
    Task<TopicSummary?> UpdateAsync(uint topicId, UpdateTopicCommand command, CancellationToken cancellationToken = default);
    Task<int> AddWordsAsync(uint topicId, IReadOnlyCollection<uint> wordIds, CancellationToken cancellationToken = default);
    Task<bool> HasActiveWordsAsync(uint topicId, CancellationToken cancellationToken = default);
    Task<bool> SetStatusAsync(uint topicId, string status, CancellationToken cancellationToken = default);
}
