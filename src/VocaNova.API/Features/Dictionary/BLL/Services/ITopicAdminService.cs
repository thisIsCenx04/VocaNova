using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public interface ITopicAdminService
{
    Task<DictionaryResult<IReadOnlyCollection<AdminTopic>>> ListAsync(AdminTopicQuery query, CancellationToken cancellationToken = default);
    Task<DictionaryResult<TopicSummary>> CreateAsync(CreateTopicCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<int>> AddWordsAsync(uint topicId, IReadOnlyCollection<uint>? wordIds, CancellationToken cancellationToken = default);
    Task<DictionaryResult<TopicSummary>> UpdateAsync(uint topicId, UpdateTopicCommand command, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> SoftDeleteAsync(uint topicId, CancellationToken cancellationToken = default);
    Task<DictionaryResult<bool>> RestoreAsync(uint topicId, CancellationToken cancellationToken = default);
}
