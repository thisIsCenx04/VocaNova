using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Features.Lists.BLL.Services.IServices;

namespace VocaNova.API.Features.Lists.BLL.Services;

public sealed class PersonalTopicQueryService : IPersonalTopicQueryService
{
    private const int MaximumPageLimit = 100;
    private readonly IPersonalTopicQueryRepository _repository;

    public PersonalTopicQueryService(IPersonalTopicQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListResult<IReadOnlyCollection<PersonalTopic>>> GetTopicsAsync(
        uint userId,
        PersonalTopicQuery query,
        CancellationToken cancellationToken = default)
    {
        if (userId == 0)
        {
            return ListResult<IReadOnlyCollection<PersonalTopic>>.Unauthorized("Unauthorized.");
        }

        var lookup = await _repository.GetTopicsAsync(userId, query.WordId, cancellationToken);
        return lookup.IsSuccess
            ? ListResult<IReadOnlyCollection<PersonalTopic>>.Success(lookup.Value!)
            : ListQueryService.MapLookupFailure<IReadOnlyCollection<PersonalTopic>>(lookup.ErrorKind);
    }

    public async Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint topicId,
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

        var lookup = await _repository.GetTopicWordsAsync(
            userId,
            topicId,
            query.Page,
            query.Limit,
            cancellationToken);
        return lookup.IsSuccess
            ? ListResult<PagedCollection<ListWord>>.Success(lookup.Value!)
            : ListQueryService.MapLookupFailure<PagedCollection<ListWord>>(lookup.ErrorKind);
    }
}
