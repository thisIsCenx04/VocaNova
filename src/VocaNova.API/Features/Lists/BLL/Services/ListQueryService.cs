using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;

namespace VocaNova.API.Features.Lists.BLL.Services;

public sealed class ListQueryService : IListQueryService
{
    private const int MaximumPageLimit = 100;
    private readonly IListQueryRepository _repository;
    private readonly IUserListCache? _cache;

    public ListQueryService(IListQueryRepository repository, IUserListCache? cache = null)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ListResult<IReadOnlyCollection<UserListSummary>>> GetListsAsync(
        uint userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<IReadOnlyCollection<UserListSummary>>.Unauthorized("Unauthorized.");
        }

        if (_cache is not null)
        {
            var cached = await _cache.GetAsync(userId, cancellationToken);
            if (cached is not null)
            {
                return ListResult<IReadOnlyCollection<UserListSummary>>.Success(cached);
            }
        }

        var lists = await _repository.GetListsAsync(userId, cancellationToken);
        if (_cache is not null)
        {
            await _cache.SetAsync(userId, lists, cancellationToken);
        }

        return ListResult<IReadOnlyCollection<UserListSummary>>.Success(lists);
    }

    public async Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint listId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<PagedCollection<ListWord>>.Unauthorized("Unauthorized.");
        }

        if (query.Page <= 0)
        {
            return ListResult<PagedCollection<ListWord>>.ValidationFailure(
                "Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > MaximumPageLimit)
        {
            return ListResult<PagedCollection<ListWord>>.ValidationFailure(
                $"Limit must be between 1 and {MaximumPageLimit}.");
        }

        var lookup = await _repository.GetOwnedListWordsAsync(
            userId,
            listId,
            query.Page,
            query.Limit,
            cancellationToken);
        return lookup.IsSuccess
            ? ListResult<PagedCollection<ListWord>>.Success(lookup.Value!)
            : MapLookupFailure<PagedCollection<ListWord>>(lookup.ErrorKind);
    }

    internal static ListResult<T> MapLookupFailure<T>(ListLookupErrorKind? errorKind) =>
        errorKind switch
        {
            ListLookupErrorKind.ListForbidden =>
                ListResult<T>.Forbidden("You do not have access to this list."),
            ListLookupErrorKind.WordNotFound => ListResult<T>.NotFound("Word not found."),
            ListLookupErrorKind.TopicNotFound => ListResult<T>.NotFound("Topic not found."),
            _ => ListResult<T>.NotFound("List not found."),
        };
}
