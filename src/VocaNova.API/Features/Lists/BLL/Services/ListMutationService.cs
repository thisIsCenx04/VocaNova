using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Constants;

namespace VocaNova.API.Features.Lists.BLL.Services;

public sealed class ListMutationService : IListMutationService
{
    private readonly IListMutationRepository _repository;
    private readonly IUserListCache? _cache;

    public ListMutationService(
        IListMutationRepository repository,
        IUserListCache? cache = null)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ListResult<UserListSummary>> CreateAsync(
        uint userId,
        CreateListCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<UserListSummary>.Unauthorized("Unauthorized.");
        }

        var listName = command.ListName.Trim();
        if (PersonalTopicListName.IsReserved(listName))
        {
            return ListResult<UserListSummary>.ValidationFailure("List name is reserved.");
        }

        if (await _repository.CountActiveAsync(userId, cancellationToken) >= AppSettings.MaxListsPerUser)
        {
            return ListResult<UserListSummary>.ValidationFailure(
                $"A user can create at most {AppSettings.MaxListsPerUser} lists.");
        }

        if (await _repository.ListNameExistsAsync(
            userId,
            listName.ToLowerInvariant(),
            cancellationToken: cancellationToken))
        {
            return ListResult<UserListSummary>.Conflict("List name already exists.");
        }

        var list = await _repository.CreateAsync(
            userId,
            new CreateListCommand(listName),
            cancellationToken);
        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<UserListSummary>.Success(list);
    }

    public async Task<ListResult<UserListSummary>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<UserListSummary>.Unauthorized("Unauthorized.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<UserListSummary>(ownership.ErrorKind);
        }

        var listName = command.ListName.Trim();
        if (PersonalTopicListName.IsReserved(listName))
        {
            return ListResult<UserListSummary>.ValidationFailure("List name is reserved.");
        }

        if (await _repository.ListNameExistsAsync(
            userId,
            listName.ToLowerInvariant(),
            listId,
            cancellationToken))
        {
            return ListResult<UserListSummary>.Conflict("List name already exists.");
        }

        var list = await _repository.UpdateAsync(
            userId,
            listId,
            new UpdateListCommand(listName),
            cancellationToken);
        if (list is null)
        {
            return ListResult<UserListSummary>.NotFound("List not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<UserListSummary>.Success(list);
    }

    public async Task<ListResult<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<bool>.Unauthorized("Unauthorized.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<bool>(ownership.ErrorKind);
        }

        if (!await _repository.SoftDeleteAsync(userId, listId, cancellationToken))
        {
            return ListResult<bool>.NotFound("List not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<bool>.Success(true);
    }

    public async Task<ListResult<ListWord>> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<ListWord>.Unauthorized("Unauthorized.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<ListWord>(ownership.ErrorKind);
        }

        if (!await _repository.ActiveWordExistsAsync(command.WordId, cancellationToken))
        {
            return ListResult<ListWord>.NotFound("Word not found.");
        }

        var normalized = command with
        {
            AddMethod = command.AddMethod.Trim(),
            Note = NormalizeNullable(command.Note),
        };
        var existing = await _repository.FindListWordAsync(
            userId,
            listId,
            command.WordId,
            cancellationToken);
        if (existing?.Status == UserStatus.Active)
        {
            return ListResult<ListWord>.Conflict("Word already exists in this list.");
        }

        var listWord = existing is null
            ? await _repository.AddWordAsync(userId, listId, normalized, cancellationToken)
            : await _repository.RestoreWordAsync(userId, listId, normalized, cancellationToken);
        if (listWord is null)
        {
            return ListResult<ListWord>.NotFound("List word not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<ListWord>.Success(listWord);
    }

    public async Task<ListResult<AddRandomListWordsResult>> AddRandomWordsAsync(
        uint userId,
        uint listId,
        AddRandomListWordsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<AddRandomListWordsResult>.Unauthorized("Unauthorized.");
        }

        if (command.Count <= 0 || command.Count > 50)
        {
            return ListResult<AddRandomListWordsResult>.ValidationFailure(
                "Count must be between 1 and 50.");
        }

        var method = command.Method?.Trim();
        if (method is not AddMethod.RandomTopic
            and not AddMethod.RandomSynonym
            and not AddMethod.RandomAntonym)
        {
            return ListResult<AddRandomListWordsResult>.ValidationFailure("Method is invalid.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<AddRandomListWordsResult>(ownership.ErrorKind);
        }

        var wordIds = method switch
        {
            AddMethod.RandomTopic => await _repository.GetRandomTopicWordIdsAsync(
                userId,
                listId,
                command.TopicId,
                command.Count,
                cancellationToken),
            AddMethod.RandomSynonym => await _repository.GetRandomRelationWordIdsAsync(
                userId,
                listId,
                "synonym",
                command.Count,
                cancellationToken),
            _ => await _repository.GetRandomRelationWordIdsAsync(
                userId,
                listId,
                "antonym",
                command.Count,
                cancellationToken),
        };

        var addedWords = new List<ListWord>();
        foreach (var wordId in wordIds)
        {
            var result = await AddWordAsync(
                userId,
                listId,
                new AddListWordCommand(wordId, method, null),
                cancellationToken);
            if (result.IsSuccess)
            {
                addedWords.Add(result.Value!);
            }
        }

        return ListResult<AddRandomListWordsResult>.Success(
            new AddRandomListWordsResult(addedWords.Count, addedWords));
    }

    public async Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<bool>.Unauthorized("Unauthorized.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<bool>(ownership.ErrorKind);
        }

        if (!await _repository.RemoveWordAsync(userId, listId, wordId, cancellationToken))
        {
            return ListResult<bool>.NotFound("List word not found.");
        }

        await RemoveCachedListsAsync(userId, cancellationToken);
        return ListResult<bool>.Success(true);
    }

    public async Task<ListResult<ListWord>> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        UpdateListWordNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<ListWord>.Unauthorized("Unauthorized.");
        }

        var ownership = await _repository.GetOwnershipAsync(userId, listId, cancellationToken);
        if (!ownership.IsSuccess)
        {
            return MapLookupFailure<ListWord>(ownership.ErrorKind);
        }

        var listWord = await _repository.UpdateNoteAsync(
            userId,
            listId,
            wordId,
            NormalizeNullable(command.Note),
            cancellationToken);
        return listWord is null
            ? ListResult<ListWord>.NotFound("List word not found.")
            : ListResult<ListWord>.Success(listWord);
    }

    private async Task RemoveCachedListsAsync(uint userId, CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            await _cache.RemoveAsync(userId, cancellationToken);
        }
    }

    private static ListResult<T> MapLookupFailure<T>(ListLookupErrorKind? errorKind) =>
        errorKind == ListLookupErrorKind.ListForbidden
            ? ListResult<T>.Forbidden("You do not have access to this list.")
            : ListResult<T>.NotFound("List not found.");

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
