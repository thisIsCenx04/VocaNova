using System.Globalization;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public sealed class WordReadService : IWordReadService
{
    private const int MaximumPageLimit = 100;

    private static readonly IReadOnlySet<string> CefrLevels = new HashSet<string>
    {
        "A1", "A2", "B1", "B2", "C1", "C2",
    };

    private readonly IWordReadRepository _wordRepository;
    private readonly IWordSearchCache? _wordSearchCache;
    private readonly IWordDetailCache? _wordDetailCache;

    public WordReadService(
        IWordReadRepository wordRepository,
        IWordSearchCache? wordSearchCache = null,
        IWordDetailCache? wordDetailCache = null)
    {
        _wordRepository = wordRepository;
        _wordSearchCache = wordSearchCache;
        _wordDetailCache = wordDetailCache;
    }

    public async Task<DictionaryResult<PagedCollection<WordSummary>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return DictionaryResult<PagedCollection<WordSummary>>.ValidationFailure(
                "Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > MaximumPageLimit)
        {
            return DictionaryResult<PagedCollection<WordSummary>>.ValidationFailure(
                $"Limit must be between 1 and {MaximumPageLimit}.");
        }

        var cefr = string.IsNullOrWhiteSpace(query.Cefr)
            ? null
            : query.Cefr.Trim().ToUpperInvariant();
        if (cefr is not null && !CefrLevels.Contains(cefr))
        {
            return DictionaryResult<PagedCollection<WordSummary>>.ValidationFailure(
                "Cefr must be one of: A1, A2, B1, B2, C1, C2.");
        }

        var normalizedQuery = string.IsNullOrWhiteSpace(query.Query)
            ? null
            : query.Query.Trim().ToLowerInvariant();
        var cacheKey = CreateSearchCacheKey(
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
                return DictionaryResult<PagedCollection<WordSummary>>.Success(cached);
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

        return DictionaryResult<PagedCollection<WordSummary>>.Success(result);
    }

    public async Task<DictionaryResult<WordDetail>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return DictionaryResult<WordDetail>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            var cached = await _wordDetailCache.GetAsync(wordId, cancellationToken);
            if (cached is not null)
            {
                return DictionaryResult<WordDetail>.Success(cached);
            }
        }

        var word = await _wordRepository.FindDetailAsync(wordId, cancellationToken);
        if (word is null)
        {
            return DictionaryResult<WordDetail>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            await _wordDetailCache.SetAsync(word, cancellationToken);
        }

        return DictionaryResult<WordDetail>.Success(word);
    }

    public async Task<DictionaryResult<WordDetail>> GetDailyAsync(
        CancellationToken cancellationToken = default)
    {
        var wordIds = await _wordRepository.GetDailyCandidateWordIdsAsync(
            requirePlayableAudio: true,
            cancellationToken);
        if (wordIds.Count == 0)
        {
            wordIds = await _wordRepository.GetDailyCandidateWordIdsAsync(
                requirePlayableAudio: false,
                cancellationToken);
        }

        if (wordIds.Count == 0)
        {
            return DictionaryResult<WordDetail>.NotFound("No daily word is available.");
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var index = Math.Abs(todayUtc.DayNumber % wordIds.Count);
        var word = await _wordRepository.FindDetailAsync(wordIds.ElementAt(index), cancellationToken);

        return word is null
            ? DictionaryResult<WordDetail>.NotFound("No daily word is available.")
            : DictionaryResult<WordDetail>.Success(word);
    }

    private static string CreateSearchCacheKey(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase)
    {
        var queryPart = string.IsNullOrWhiteSpace(normalizedQuery) ? "_" : normalizedQuery;
        var cefrPart = string.IsNullOrWhiteSpace(cefr) ? "_" : cefr;
        var topicPart = topicId?.ToString(CultureInfo.InvariantCulture) ?? "_";
        var phrasePart = isPhrase?.ToString() ?? "_";
        return $"word-search:{queryPart}:{page}:{limit}:{cefrPart}:{topicPart}:{phrasePart}";
    }
}
