using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Services;

public interface IListMutationService
{
    Task<ListResult<UserListSummary>> CreateAsync(
        uint userId,
        CreateListCommand command,
        CancellationToken cancellationToken = default);

    Task<ListResult<UserListSummary>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListCommand command,
        CancellationToken cancellationToken = default);

    Task<ListResult<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default);

    Task<ListResult<ListWord>> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordCommand command,
        CancellationToken cancellationToken = default);

    Task<ListResult<AddRandomListWordsResult>> AddRandomWordsAsync(
        uint userId,
        uint listId,
        AddRandomListWordsCommand command,
        CancellationToken cancellationToken = default);

    Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<ListResult<ListWord>> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        UpdateListWordNoteCommand command,
        CancellationToken cancellationToken = default);
}
