using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Features.Lists.BLL.Services;
using VocaNova.API.Features.Lists.Contracts.Requests;
using VocaNova.API.Features.Lists.DAL.Repositories;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.Tests.Lists;

internal sealed record ListWordsQuery(int Page = 1, int Limit = 20)
{
    public VocaNova.API.Features.Lists.BLL.Models.ListWordsQuery ToBusiness() => new(Page, Limit);
}

internal sealed class UserListRepository
{
    public UserListRepository(VocaNovaDbContext dbContext)
    {
        Query = new ListQueryRepository(dbContext);
        Mutation = new ListMutationRepository(dbContext);
    }

    public ListQueryRepository Query { get; }
    public ListMutationRepository Mutation { get; }
}

internal sealed class PersonalTopicRepository
{
    public PersonalTopicRepository(VocaNovaDbContext dbContext)
    {
        Query = new PersonalTopicQueryRepository(dbContext);
        Mutation = new PersonalTopicMutationRepository(dbContext);
    }

    public PersonalTopicQueryRepository Query { get; }
    public PersonalTopicMutationRepository Mutation { get; }
}

internal sealed class UserListService
{
    private readonly ListQueryService _query;
    private readonly ListMutationService _mutation;

    public UserListService(UserListRepository repository, IUserListCache? cache = null)
    {
        _query = new ListQueryService(repository.Query, cache);
        _mutation = new ListMutationService(repository.Mutation, cache);
    }

    public Task<ListResult<IReadOnlyCollection<UserListDto>>> GetByUserAsync(
        uint userId,
        CancellationToken cancellationToken = default) =>
        _query.GetListsAsync(userId, cancellationToken);

    public Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint listId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default) =>
        _query.GetWordsAsync(userId, listId, query.ToBusiness(), cancellationToken);

    public Task<ListResult<UserListDto>> CreateAsync(
        uint userId,
        CreateListRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.CreateAsync(userId, request.ToBusinessCommand(), cancellationToken);

    public Task<ListResult<UserListDto>> UpdateAsync(
        uint userId,
        uint listId,
        UpdateListRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.UpdateAsync(userId, listId, request.ToBusinessCommand(), cancellationToken);

    public Task<ListResult<bool>> SoftDeleteAsync(
        uint userId,
        uint listId,
        CancellationToken cancellationToken = default) =>
        _mutation.SoftDeleteAsync(userId, listId, cancellationToken);

    public Task<ListResult<ListWord>> AddWordAsync(
        uint userId,
        uint listId,
        AddListWordRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.AddWordAsync(userId, listId, request.ToBusinessCommand(), cancellationToken);

    public Task<ListResult<AddRandomListWordsResult>> AddRandomWordsAsync(
        uint userId,
        uint listId,
        AddRandomListWordsCommand command,
        CancellationToken cancellationToken = default) =>
        _mutation.AddRandomWordsAsync(userId, listId, command, cancellationToken);

    public Task<ListResult<AddRandomListWordsResult>> AddRandomWordsAsync(
        uint userId,
        uint listId,
        AddRandomListWordsRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.AddRandomWordsAsync(userId, listId, request.ToBusinessCommand(), cancellationToken);

    public Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint listId,
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _mutation.RemoveWordAsync(userId, listId, wordId, cancellationToken);

    public Task<ListResult<ListWord>> UpdateWordNoteAsync(
        uint userId,
        uint listId,
        uint wordId,
        UpdateListWordNoteRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.UpdateWordNoteAsync(userId, listId, wordId, request.ToBusinessCommand(), cancellationToken);
}

internal sealed class PersonalTopicService
{
    private readonly PersonalTopicQueryService _query;
    private readonly PersonalTopicMutationService _mutation;

    public PersonalTopicService(
        UserListRepository listRepository,
        PersonalTopicRepository personalTopicRepository,
        IUserListCache? cache = null)
    {
        _query = new PersonalTopicQueryService(personalTopicRepository.Query);
        _mutation = new PersonalTopicMutationService(
            personalTopicRepository.Mutation,
            listRepository.Mutation,
            cache);
    }

    public PersonalTopicService(
        PersonalTopicRepository personalTopicRepository,
        UserListRepository listRepository,
        IUserListCache? cache = null)
        : this(listRepository, personalTopicRepository, cache)
    {
    }

    public Task<ListResult<IReadOnlyCollection<PersonalTopic>>> GetTopicsAsync(
        uint userId,
        uint? wordId = null,
        CancellationToken cancellationToken = default) =>
        _query.GetTopicsAsync(
            userId,
            new PersonalTopicQuery(wordId),
            cancellationToken);

    public Task<ListResult<PagedCollection<ListWord>>> GetWordsAsync(
        uint userId,
        uint topicId,
        ListWordsQuery query,
        CancellationToken cancellationToken = default) =>
        _query.GetWordsAsync(userId, topicId, query.ToBusiness(), cancellationToken);

    public Task<ListResult<PersonalTopic>> AddWordAsync(
        uint userId,
        uint topicId,
        AddPersonalTopicWordRequest request,
        CancellationToken cancellationToken = default) =>
        _mutation.AddWordAsync(userId, topicId, request.ToBusinessCommand(), cancellationToken);

    public Task<ListResult<bool>> RemoveWordAsync(
        uint userId,
        uint topicId,
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _mutation.RemoveWordAsync(userId, topicId, wordId, cancellationToken);
}
