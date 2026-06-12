using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Dictionary.Services;

public sealed class WordService : IWordService
{
    private readonly IWordRepository _wordRepository;
    private readonly IWordSearchCache? _wordSearchCache;

    public WordService(
        IWordRepository wordRepository,
        IWordSearchCache? wordSearchCache = null)
    {
        _wordRepository = wordRepository;
        _wordSearchCache = wordSearchCache;
    }

    public async Task<Result<PagedResult<WordSummaryDto>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<WordSummaryDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<WordSummaryDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cefr = string.IsNullOrWhiteSpace(query.Cefr)
            ? null
            : query.Cefr.Trim().ToUpperInvariant();
        if (cefr is not null && !CefrLevel.All.Contains(cefr))
        {
            return Result<PagedResult<WordSummaryDto>>.Fail("Cefr must be one of: A1, A2, B1, B2, C1, C2.");
        }

        var normalizedQuery = string.IsNullOrWhiteSpace(query.Q)
            ? null
            : query.Q.NormalizeWord();

        var cacheKey = WordSearchCacheKey.Create(
            normalizedQuery,
            query.Page,
            query.Limit,
            cefr,
            query.TopicId,
            query.IsPhrase);

        if (_wordSearchCache is not null)
        {
            var cached = await _wordSearchCache.GetAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Result<PagedResult<WordSummaryDto>>.Ok(cached);
            }
        }

        var result = await _wordRepository.SearchAsync(
            normalizedQuery,
            query.Page,
            query.Limit,
            cefr,
            query.TopicId,
            query.IsPhrase,
            cancellationToken);

        if (_wordSearchCache is not null)
        {
            await _wordSearchCache.SetAsync(cacheKey, result, cancellationToken);
        }

        return Result<PagedResult<WordSummaryDto>>.Ok(result);
    }
}
