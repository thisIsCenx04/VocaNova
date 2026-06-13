using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.DTOs;

namespace VocaNova.API.Features.Lists.Services;

public interface IUserListService
{
    Task<Result<IReadOnlyCollection<UserListDto>>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserListDto>> CreateAsync(
        uint userId,
        CreateListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserListDto>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<ListWordDto>>> GetWordsAsync(
        uint userId,
        uint listId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ListWordDto>> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AddRandomListWordsResultDto>> AddRandomWordsAsync(
        uint userId,
        uint listId,
        AddRandomListWordsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<Result<ListWordDto>> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        UpdateListWordNoteRequest request,
        CancellationToken cancellationToken = default);
}
