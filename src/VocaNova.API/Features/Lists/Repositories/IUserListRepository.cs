using VocaNova.API.Features.Lists.DTOs;
using VocaNova.API.Common.Results;

namespace VocaNova.API.Features.Lists.Repositories;

public interface IUserListRepository
{
    Task<IReadOnlyCollection<UserListDto>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<bool> ListNameExistsAsync(
        uint userId,
        string normalizedListName,
        uint? excludingListId = null,
        CancellationToken cancellationToken = default);

    Task<UserListDto> CreateAsync(
        uint userId,
        string listName,
        CancellationToken cancellationToken = default);

    Task<UserListOwnershipDto?> FindOwnershipAsync(
        uint listId,
        CancellationToken cancellationToken = default);

    Task<UserListDto?> UpdateAsync(
        uint listId,
        string listName,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteWithWordsAsync(
        uint listId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ListWordDto>> GetWordsAsync(
        uint userId,
        uint listId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveWordExistsAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListWordStateDto?> FindListWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListWordDto> AddWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        string addMethod,
        string? note,
        CancellationToken cancellationToken = default);

    Task<ListWordDto?> RestoreWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        string addMethod,
        string? note,
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

    Task<bool> SoftDeleteWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListWordDto?> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        string? note,
        CancellationToken cancellationToken = default);
}
