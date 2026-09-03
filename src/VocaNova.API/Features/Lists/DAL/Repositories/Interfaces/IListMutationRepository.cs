using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Abstractions;

public interface IListMutationRepository
{
    Task<int> CountActiveAsync(uint userId, CancellationToken cancellationToken = default);

    Task<bool> ListNameExistsAsync(
        uint userId,
        string normalizedListName,
        uint? excludingListId = null,
        CancellationToken cancellationToken = default);

    Task<UserListSummary> CreateAsync(
        uint userId,
        CreateListCommand command,
        CancellationToken cancellationToken = default);

    Task<ListLookupResult<ListOwnership>> GetOwnershipAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default);

    Task<UserListSummary?> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveWordExistsAsync(uint wordId, CancellationToken cancellationToken = default);

    Task<ListWordState?> FindListWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListWord> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default);

    Task<ListWord?> RestoreWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetRandomTopicWordIdsAsync(
        uint userId,
        uint listId,
        uint? topicId,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<uint>> GetRandomRelationWordIdsAsync(
        uint userId,
        uint listId,
        string relationType,
        int count,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListWord?> UpdateNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        string? note,
        CancellationToken cancellationToken = default);
}
