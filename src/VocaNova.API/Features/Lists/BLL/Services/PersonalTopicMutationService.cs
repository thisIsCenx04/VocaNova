using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Lists.BLL.Services.IServices;

namespace VocaNova.API.Features.Lists.BLL.Services;

public sealed class PersonalTopicMutationService : IPersonalTopicMutationService
{
    private readonly IPersonalTopicMutationRepository _personalTopicRepository;
    private readonly IListMutationRepository _listRepository;
    private readonly IUserListCache? _cache;

    public PersonalTopicMutationService(
        IPersonalTopicMutationRepository personalTopicRepository,
        IListMutationRepository listRepository,
        IUserListCache? cache = null)
    {
        _personalTopicRepository = personalTopicRepository;
        _listRepository = listRepository;
        _cache = cache;
    }

    public async Task<ListResult<PersonalTopic>> AddWordAsync(
        uint userId,
        uint topicId,
        AddPersonalTopicWordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<PersonalTopic>.Unauthorized("Unauthorized.");
        }

        if (!await _personalTopicRepository.TopicExistsAsync(topicId, cancellationToken))
        {
            return ListResult<PersonalTopic>.NotFound("Topic not found.");
        }

        if (!await _listRepository.ActiveWordExistsAsync(command.WordId, cancellationToken))
        {
            return ListResult<PersonalTopic>.NotFound("Word not found.");
        }

        if (!await _personalTopicRepository.WordBelongsToTopicAsync(
            topicId,
            command.WordId,
            cancellationToken))
        {
            return ListResult<PersonalTopic>.ValidationFailure(
                "Word does not belong to this system topic.");
        }

        // CURRENT compatibility: this call saves the reserved list independently.
        var listId = await _personalTopicRepository.GetOrCreateListIdAsync(
            userId,
            topicId,
            cancellationToken);
        var existing = await _listRepository.FindListWordAsync(
            userId,
            listId,
            command.WordId,
            cancellationToken);
        if (existing?.Status == UserStatus.Active)
        {
            return ListResult<PersonalTopic>.Conflict(
                "Word already exists in this personal topic.");
        }

        var listWordCommand = new AddListWordCommand(
            command.WordId,
            AddMethod.Search,
            NormalizeNullable(command.Note));
        if (existing is null)
        {
            await _listRepository.AddWordAsync(userId, listId, listWordCommand, cancellationToken);
        }
        else
        {
            await _listRepository.RestoreWordAsync(userId, listId, listWordCommand, cancellationToken);
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        var topics = await _personalTopicRepository.GetTopicsAsync(
            userId,
            command.WordId,
            cancellationToken);
        return ListResult<PersonalTopic>.Success(topics.Single(item => item.TopicId == topicId));
    }

    public async Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<bool>.Unauthorized("Unauthorized.");
        }

        if (!await _personalTopicRepository.TopicExistsAsync(topicId, cancellationToken))
        {
            return ListResult<bool>.NotFound("Topic not found.");
        }

        var listId = await _personalTopicRepository.FindActiveListIdAsync(
            userId,
            topicId,
            cancellationToken);
        if (!listId.HasValue)
        {
            return ListResult<bool>.NotFound("Personal topic word not found.");
        }

        var existing = await _listRepository.FindListWordAsync(
            userId,
            listId.Value,
            wordId,
            cancellationToken);
        if (existing?.Status != UserStatus.Active)
        {
            return ListResult<bool>.NotFound("Personal topic word not found.");
        }

        if (!await _listRepository.RemoveWordAsync(
            userId,
            listId.Value,
            wordId,
            cancellationToken))
        {
            return ListResult<bool>.NotFound("Personal topic word not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<bool>.Success(true);
    }

    private async Task RemoveCachedListsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            await _cache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
