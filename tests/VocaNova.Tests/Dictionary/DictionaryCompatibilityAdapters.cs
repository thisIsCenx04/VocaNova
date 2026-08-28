using Microsoft.AspNetCore.Http;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Services;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.DAL.Repositories;
using VocaNova.API.Features.Lists.BLL.Abstractions;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.Tests.Dictionary;

internal sealed record WordSearchQuery(string? Query = null, int Page = 1, int Limit = 20)
{
    public WordSearchQuery(
        string? query,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        int page,
        int limit)
        : this(query, page, limit)
    {
        Cefr = cefr;
        TopicId = topicId;
        IsPhrase = isPhrase;
    }

    public string? Cefr { get; init; }
    public uint? TopicId { get; init; }
    public bool? IsPhrase { get; init; }

    public VocaNova.API.Features.Dictionary.BLL.Models.WordSearchQuery ToBusiness() =>
        new(Query, Page, Limit, Cefr, TopicId, IsPhrase);
}

internal sealed record AdminWordQuery
{
    public AdminWordQuery()
    {
    }

    public AdminWordQuery(
        string? q,
        string? cefr,
        uint? topicId,
        string? wordType,
        string? status,
        bool includeDeleted,
        int page,
        int limit,
        string? sortBy,
        string? sortDirection)
    {
        Q = q;
        Cefr = cefr;
        TopicId = topicId;
        WordType = wordType;
        Status = status;
        IncludeDeleted = includeDeleted;
        Page = page;
        Limit = limit;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public string? Q { get; init; }
    public string? Cefr { get; init; }
    public uint? TopicId { get; init; }
    public string? WordType { get; init; }
    public string? Status { get; init; }
    public bool IncludeDeleted { get; init; }
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }

    public VocaNova.API.Features.Dictionary.BLL.Models.AdminWordQuery ToBusiness() =>
        new(Q, Cefr, TopicId, WordType, Status, IncludeDeleted, Page, Limit, SortBy, SortDirection);
}

internal sealed record AdminTopicQuery
{
    public string? Q { get; init; }
    public string? Status { get; init; }
    public bool IncludeDeleted { get; init; }

    public VocaNova.API.Features.Dictionary.BLL.Models.AdminTopicQuery ToBusiness() =>
        new(Q, Status, IncludeDeleted);
}

internal sealed record TopicWordsQuery
{
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;

    public VocaNova.API.Features.Dictionary.BLL.Models.TopicWordsQuery ToBusiness() =>
        new(Page, Limit);
}

internal sealed class WordRepository
{
    public WordRepository(VocaNovaDbContext dbContext)
    {
        Read = new WordReadRepository(dbContext);
        Admin = new WordAdminRepository(dbContext);
    }

    public WordReadRepository Read { get; }
    public WordAdminRepository Admin { get; }
}

internal sealed class WordService
{
    private readonly WordReadService _read;
    private readonly WordAdminService _admin;

    public WordService(WordRepository repository, IWordSearchCache? wordSearchCache = null)
    {
        _read = new WordReadService(repository.Read, wordSearchCache);
        _admin = new WordAdminService(repository.Admin);
    }

    public WordService(WordRepository repository, IWordDetailCache? wordDetailCache)
    {
        _read = new WordReadService(repository.Read, wordDetailCache: wordDetailCache);
        _admin = new WordAdminService(repository.Admin, wordDetailCache);
    }

    public WordService(
        WordRepository repository,
        IWordDetailCache? wordDetailCache,
        IWordAudioStorage? audioStorage,
        IWordImageStorage? imageStorage,
        IUserListCache? userListCache)
    {
        _read = new WordReadService(repository.Read, wordDetailCache: wordDetailCache);
        _admin = new WordAdminService(
            repository.Admin,
            wordDetailCache: wordDetailCache,
            audioStorage: audioStorage,
            imageStorage: imageStorage,
            userListCache: userListCache);
    }

    public Task<DictionaryResult<PagedCollection<WordSummaryDto>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default) =>
        _read.SearchAsync(query.ToBusiness(), cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _read.GetByIdAsync(wordId, cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> GetDailyAsync(
        CancellationToken cancellationToken = default) =>
        _read.GetDailyAsync(cancellationToken);

    public Task<DictionaryResult<PagedCollection<AdminWordListItem>>> SearchAdminAsync(
        AdminWordQuery query,
        CancellationToken cancellationToken = default) =>
        _admin.SearchAsync(query.ToBusiness(), cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> CreateAsync(
        CreateWordRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.CreateAsync(request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> UpdateAsync(
        uint wordId,
        UpdateWordRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.UpdateAsync(wordId, request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<BulkImportResult>> ImportCsvAsync(
        IFormFile? file,
        CancellationToken cancellationToken = default) =>
        _admin.ImportCsvAsync(file.ToUploadedContent(file?.OpenReadStream()), cancellationToken);

    public Task<DictionaryResult<bool>> SoftDeleteAsync(
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _admin.SoftDeleteAsync(wordId, cancellationToken);

    public Task<DictionaryResult<bool>> RestoreAsync(
        uint wordId,
        CancellationToken cancellationToken = default) =>
        _admin.RestoreAsync(wordId, cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> UploadImageAsync(
        uint wordId,
        VocaNova.API.Features.Dictionary.BLL.Models.UploadedContent? content,
        CancellationToken cancellationToken = default) =>
        _admin.UploadImageAsync(wordId, content, cancellationToken);

    public Task<DictionaryResult<WordDetailDto>> UpdateImageUrlAsync(
        uint wordId,
        UpdateWordImageRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.UpdateImageUrlAsync(wordId, request.ImageUrl, cancellationToken);

    public Task<DictionaryResult<WordAudioDto>> UploadAudioAsync(
        uint wordId,
        string? accent,
        VocaNova.API.Features.Dictionary.BLL.Models.UploadedContent? content,
        CancellationToken cancellationToken = default) =>
        _admin.UploadAudioAsync(wordId, accent, content, cancellationToken);

    public Task<DictionaryResult<bool>> SoftDeleteAudioAsync(
        uint wordId,
        uint audioId,
        CancellationToken cancellationToken = default) =>
        _admin.SoftDeleteAudioAsync(wordId, audioId, cancellationToken);

    public Task<DictionaryResult<WordSenseDto>> CreateSenseAsync(
        uint wordId,
        CreateSenseRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.CreateSenseAsync(wordId, request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<WordSenseDto>> UpdateSenseAsync(
        uint wordId,
        uint senseId,
        UpdateSenseRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.UpdateSenseAsync(wordId, senseId, request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<bool>> SoftDeleteSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default) =>
        _admin.SoftDeleteSenseAsync(wordId, senseId, cancellationToken);

    public Task<DictionaryResult<bool>> RestoreSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default) =>
        _admin.RestoreSenseAsync(wordId, senseId, cancellationToken);
}

internal sealed class TopicRepository
{
    public TopicRepository(VocaNovaDbContext dbContext)
    {
        Read = new TopicReadRepository(dbContext);
        Admin = new TopicAdminRepository(dbContext);
    }

    public TopicReadRepository Read { get; }
    public TopicAdminRepository Admin { get; }
}

internal sealed class TopicService
{
    private readonly TopicReadService _read;
    private readonly TopicAdminService _admin;

    public TopicService(
        TopicRepository topicRepository,
        WordRepository wordRepository,
        ITopicCache? topicCache = null)
    {
        _read = new TopicReadService(topicRepository.Read, wordRepository.Read, topicCache);
        _admin = new TopicAdminService(topicRepository.Admin, topicCache);
    }

    public Task<DictionaryResult<IReadOnlyCollection<TopicSummaryDto>>> GetTopicsAsync(
        CancellationToken cancellationToken = default) =>
        _read.GetTopicsAsync(cancellationToken);

    public Task<DictionaryResult<PagedCollection<WordSummaryDto>>> GetWordsAsync(
        uint topicId,
        TopicWordsQuery query,
        CancellationToken cancellationToken = default) =>
        _read.GetWordsAsync(topicId, query.ToBusiness(), cancellationToken);

    public Task<DictionaryResult<IReadOnlyCollection<AdminTopic>>> GetAdminTopicsAsync(
        AdminTopicQuery query,
        CancellationToken cancellationToken = default) =>
        _admin.ListAsync(query.ToBusiness(), cancellationToken);

    public Task<DictionaryResult<TopicSummaryDto>> CreateAsync(
        CreateTopicRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.CreateAsync(request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<TopicSummaryDto>> UpdateAsync(
        uint topicId,
        UpdateTopicRequest request,
        CancellationToken cancellationToken = default) =>
        _admin.UpdateAsync(topicId, request.ToBusinessCommand(), cancellationToken);

    public Task<DictionaryResult<bool>> SoftDeleteAsync(
        uint topicId,
        CancellationToken cancellationToken = default) =>
        _admin.SoftDeleteAsync(topicId, cancellationToken);

    public Task<DictionaryResult<bool>> RestoreAsync(
        uint topicId,
        CancellationToken cancellationToken = default) =>
        _admin.RestoreAsync(topicId, cancellationToken);
}
