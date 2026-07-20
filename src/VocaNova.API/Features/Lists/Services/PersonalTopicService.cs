using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Features.Lists.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Lists.Services;

public sealed class PersonalTopicService : IPersonalTopicService
{
    private readonly IPersonalTopicRepository _personalTopicRepository;
    private readonly IUserListRepository _userListRepository;
    private readonly IUserListCache? _userListCache;

    public PersonalTopicService(
        IPersonalTopicRepository personalTopicRepository,
        IUserListRepository userListRepository,
        IUserListCache? userListCache = null)
    {
        _personalTopicRepository = personalTopicRepository;
        _userListRepository = userListRepository;
        _userListCache = userListCache;
    }

    public async Task<Result<IReadOnlyCollection<PersonalTopicDto>>> GetTopicsAsync(
        uint userId,
        uint? wordId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<IReadOnlyCollection<PersonalTopicDto>>.Unauthorized("Unauthorized.");
        }

        if (wordId.HasValue
            && !await _userListRepository.ActiveWordExistsAsync(wordId.Value, cancellationToken))
        {
            return Result<IReadOnlyCollection<PersonalTopicDto>>.NotFound("Word not found.");
        }

        var topics = await _personalTopicRepository.GetTopicsAsync(userId, wordId, cancellationToken);
        return Result<IReadOnlyCollection<PersonalTopicDto>>.Ok(topics);
    }

    public async Task<Result<PagedResult<ListWordDto>>> GetWordsAsync(
        uint userId,
        uint topicId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<PagedResult<ListWordDto>>.Unauthorized("Unauthorized.");
        }

        if (query.Page <= 0)
        {
            return Result<PagedResult<ListWordDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<ListWordDto>>.Fail(
                $"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        if (!await _personalTopicRepository.TopicExistsAsync(topicId, cancellationToken))
        {
            return Result<PagedResult<ListWordDto>>.NotFound("Topic not found.");
        }

        var listId = await _personalTopicRepository.FindActiveListIdAsync(
            userId,
            topicId,
            cancellationToken);
        if (!listId.HasValue)
        {
            return Result<PagedResult<ListWordDto>>.Ok(
                new PagedResult<ListWordDto>(Array.Empty<ListWordDto>(), query.Page, query.Limit, 0));
        }

        var words = await _userListRepository.GetWordsAsync(
            userId,
            listId.Value,
            query.Page,
            query.Limit,
            cancellationToken);
        return Result<PagedResult<ListWordDto>>.Ok(words);
    }

    public async Task<Result<PersonalTopicDto>> AddWordAsync(
        uint userId,
        uint topicId,
        AddPersonalTopicWordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<PersonalTopicDto>.Unauthorized("Unauthorized.");
        }

        if (!await _personalTopicRepository.TopicExistsAsync(topicId, cancellationToken))
        {
            return Result<PersonalTopicDto>.NotFound("Topic not found.");
        }

        if (!await _userListRepository.ActiveWordExistsAsync(request.WordId, cancellationToken))
        {
            return Result<PersonalTopicDto>.NotFound("Word not found.");
        }

        if (!await _personalTopicRepository.WordBelongsToTopicAsync(
            topicId,
            request.WordId,
            cancellationToken))
        {
            return Result<PersonalTopicDto>.Fail("Word does not belong to this system topic.");
        }

        var listId = await _personalTopicRepository.GetOrCreateListIdAsync(
            userId,
            topicId,
            cancellationToken);
        var existing = await _userListRepository.FindListWordAsync(
            userId,
            listId,
            request.WordId,
            cancellationToken);
        if (existing?.Status == UserStatus.Active)
        {
            return Result<PersonalTopicDto>.Conflict("Word already exists in this personal topic.");
        }

        var note = NormalizeNullable(request.Note);
        if (existing is null)
        {
            await _userListRepository.AddWordAsync(
                userId,
                listId,
                request.WordId,
                AddMethod.Search,
                note,
                cancellationToken);
        }
        else
        {
            await _userListRepository.RestoreWordAsync(
                userId,
                listId,
                request.WordId,
                AddMethod.Search,
                note,
                cancellationToken);
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        var topics = await _personalTopicRepository.GetTopicsAsync(
            userId,
            request.WordId,
            cancellationToken);
        var topic = topics.Single(item => item.TopicId == topicId);
        return Result<PersonalTopicDto>.Ok(topic);
    }

    public async Task<Result<bool>> RemoveWordAsync(
        uint userId,
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return Result<bool>.Unauthorized("Unauthorized.");
        }

        if (!await _personalTopicRepository.TopicExistsAsync(topicId, cancellationToken))
        {
            return Result<bool>.NotFound("Topic not found.");
        }

        var listId = await _personalTopicRepository.FindActiveListIdAsync(
            userId,
            topicId,
            cancellationToken);
        if (!listId.HasValue)
        {
            return Result<bool>.NotFound("Personal topic word not found.");
        }

        var existing = await _userListRepository.FindListWordAsync(
            userId,
            listId.Value,
            wordId,
            cancellationToken);
        if (existing?.Status != UserStatus.Active)
        {
            return Result<bool>.NotFound("Personal topic word not found.");
        }

        var deleted = await _userListRepository.SoftDeleteWordAsync(
            userId,
            listId.Value,
            wordId,
            cancellationToken);
        if (!deleted)
        {
            return Result<bool>.NotFound("Personal topic word not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return Result<bool>.Ok(true);
    }

    private async Task RemoveCachedListsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_userListCache is not null)
        {
            await _userListCache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
