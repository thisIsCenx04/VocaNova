using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Abstractions;

public interface IPersonalTopicMutationRepository
{
    Task<IReadOnlyCollection<PersonalTopic>> GetTopicsAsync(
        uint userId,
        uint? wordId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TopicExistsAsync(uint topicId, CancellationToken cancellationToken = default);

    Task<bool> WordBelongsToTopicAsync(
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<uint?> FindActiveListIdAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default);

    Task<uint> GetOrCreateListIdAsync(
        uint userId,
        uint topicId,
        CancellationToken cancellationToken = default);
}
