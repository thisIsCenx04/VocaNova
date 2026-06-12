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
    private readonly IWordDetailCache? _wordDetailCache;

    public WordService(
        IWordRepository wordRepository,
        IWordSearchCache? wordSearchCache = null,
        IWordDetailCache? wordDetailCache = null)
    {
        _wordRepository = wordRepository;
        _wordSearchCache = wordSearchCache;
        _wordDetailCache = wordDetailCache;
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

    public async Task<Result<WordDetailDto>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            var cached = await _wordDetailCache.GetAsync(wordId, cancellationToken);
            if (cached is not null)
            {
                return Result<WordDetailDto>.Ok(cached);
            }
        }

        var word = await _wordRepository.FindDetailAsync(wordId, cancellationToken);
        if (word is null)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            await _wordDetailCache.SetAsync(word, cancellationToken);
        }

        return Result<WordDetailDto>.Ok(word);
    }

    public async Task<Result<WordDetailDto>> CreateAsync(
        CreateWordRequest request,
        CancellationToken cancellationToken = default)
    {
        var rawWord = request.Word!;
        var wordKey = rawWord.NormalizeWord();
        if (await _wordRepository.WordKeyExistsAsync(wordKey, cancellationToken: cancellationToken))
        {
            return Result<WordDetailDto>.Conflict("Word already exists.");
        }

        var normalizedRequest = request with
        {
            Word = rawWord.Trim(),
            Cefr = NormalizeCefr(request.Cefr),
            PhoneticUk = NormalizeNullable(request.PhoneticUk),
            PhoneticUs = NormalizeNullable(request.PhoneticUs),
            ImageUrl = NormalizeNullable(request.ImageUrl),
        };

        var word = await _wordRepository.CreateAsync(normalizedRequest, wordKey, cancellationToken);
        return Result<WordDetailDto>.Ok(word);
    }

    public async Task<Result<WordDetailDto>> UpdateAsync(
        uint wordId,
        UpdateWordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        var rawWord = request.Word!;
        var wordKey = rawWord.NormalizeWord();
        if (await _wordRepository.WordKeyExistsAsync(wordKey, wordId, cancellationToken))
        {
            return Result<WordDetailDto>.Conflict("Word already exists.");
        }

        var normalizedRequest = request with
        {
            Word = rawWord.Trim(),
            Cefr = NormalizeCefr(request.Cefr),
            PhoneticUk = NormalizeNullable(request.PhoneticUk),
            PhoneticUs = NormalizeNullable(request.PhoneticUs),
            ImageUrl = NormalizeNullable(request.ImageUrl),
        };

        var word = await _wordRepository.UpdateMetadataAsync(
            wordId,
            normalizedRequest,
            wordKey,
            cancellationToken);
        if (word is null)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            await _wordDetailCache.RemoveAsync(wordId, cancellationToken);
        }

        return Result<WordDetailDto>.Ok(word);
    }

    public async Task<Result<bool>> SoftDeleteAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return await SetWordStatusAsync(wordId, UserStatus.Deleted, cancellationToken);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return await SetWordStatusAsync(wordId, UserStatus.Active, cancellationToken);
    }

    private async Task<Result<bool>> SetWordStatusAsync(
        uint wordId,
        string status,
        CancellationToken cancellationToken)
    {
        if (wordId == 0)
        {
            return Result<bool>.NotFound("Word not found.");
        }

        var updated = await _wordRepository.SetStatusAsync(wordId, status, cancellationToken);
        if (!updated)
        {
            return Result<bool>.NotFound("Word not found.");
        }

        if (_wordDetailCache is not null)
        {
            await _wordDetailCache.RemoveAsync(wordId, cancellationToken);
        }

        return Result<bool>.Ok(true);
    }

    private static string? NormalizeCefr(string? cefr)
    {
        return string.IsNullOrWhiteSpace(cefr) ? null : cefr.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
