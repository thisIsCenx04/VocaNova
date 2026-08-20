using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Services;

public interface IPersonalTopicMutationService
{
    Task<ListResult<PersonalTopic>> AddWordAsync(
        uint userId,
        uint topicId,
        AddPersonalTopicWordCommand command,
        CancellationToken cancellationToken = default);

    Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default);
}
