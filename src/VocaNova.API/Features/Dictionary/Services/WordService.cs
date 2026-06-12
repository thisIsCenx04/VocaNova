using System.Text;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Infrastructure.Caching;

namespace VocaNova.API.Features.Dictionary.Services;

public sealed class WordService : IWordService
{
    private const string CsvWordColumn = "word";
    private const string CsvCefrColumn = "cefr_level";
    private const string CsvPhoneticUkColumn = "phonetic_uk";
    private const string CsvPhoneticUsColumn = "phonetic_us";
    private const string CsvWordClassColumn = "word_class";
    private const string CsvEnglishDefinitionColumn = "english_definition";
    private const string CsvVietnameseMeaningColumn = "vietnamese_meaning";

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

    public async Task<Result<BulkImportResultDto>> ImportCsvAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return Result<BulkImportResultDto>.Fail("CSV file is required.");
        }

        var errors = new List<BulkImportErrorDto>();
        var knownWordIds = new Dictionary<string, uint>(StringComparer.Ordinal);
        var importedWords = 0;
        var importedSenses = 0;
        var skipped = 0;

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return Result<BulkImportResultDto>.Fail("CSV header is required.");
        }

        var headerIndexes = BuildHeaderIndexes(ParseCsvLine(headerLine));
        var missingColumn = RequiredCsvColumns()
            .FirstOrDefault(column => !headerIndexes.ContainsKey(column));
        if (missingColumn is not null)
        {
            return Result<BulkImportResultDto>.Fail($"CSV header must include '{missingColumn}'.");
        }

        var rowNumber = 1;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = ParseCsvLine(line);
            var row = new CsvImportRow(
                GetCell(cells, headerIndexes, CsvWordColumn),
                GetCell(cells, headerIndexes, CsvCefrColumn),
                GetCell(cells, headerIndexes, CsvPhoneticUkColumn),
                GetCell(cells, headerIndexes, CsvPhoneticUsColumn),
                GetCell(cells, headerIndexes, CsvWordClassColumn),
                GetCell(cells, headerIndexes, CsvEnglishDefinitionColumn),
                GetCell(cells, headerIndexes, CsvVietnameseMeaningColumn));

            if (!TryValidateImportRow(row, rowNumber, errors))
            {
                skipped++;
                continue;
            }

            var wordKey = row.Word.NormalizeWord();
            if (!knownWordIds.TryGetValue(wordKey, out var wordId))
            {
                wordId = await _wordRepository.FindWordIdByKeyAsync(wordKey, cancellationToken) ?? 0;
            }

            if (wordId == 0)
            {
                var word = await _wordRepository.CreateWithSenseAsync(
                    new CreateWordRequest(
                        row.Word.Trim(),
                        NormalizeCefr(row.Cefr),
                        NormalizeNullable(row.PhoneticUk),
                        NormalizeNullable(row.PhoneticUs),
                        null),
                    wordKey,
                    new CreateSenseRequest(
                        1,
                        row.WordClass.Trim(),
                        row.EnglishDefinition.Trim(),
                        NormalizeNullable(row.VietnameseMeaning)),
                    cancellationToken);

                knownWordIds[wordKey] = word.WordId;
                importedWords++;
                importedSenses++;
                continue;
            }

            knownWordIds[wordKey] = wordId;

            var sense = await _wordRepository.CreateNextSenseAsync(
                wordId,
                row.WordClass,
                row.EnglishDefinition,
                row.VietnameseMeaning,
                cancellationToken);
            if (sense is null)
            {
                errors.Add(new BulkImportErrorDto(rowNumber, CsvWordColumn, "Word not found."));
                skipped++;
                continue;
            }

            await RemoveCachedWordAsync(wordId, cancellationToken);
            importedSenses++;
        }

        return Result<BulkImportResultDto>.Ok(new BulkImportResultDto(
            importedWords,
            importedSenses,
            skipped,
            errors));
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

    public async Task<Result<WordSenseDto>> CreateSenseAsync(
        uint wordId,
        CreateSenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var sense = await _wordRepository.CreateSenseAsync(
            wordId,
            NormalizeCreateSenseRequest(request),
            cancellationToken);
        if (sense is null)
        {
            return Result<WordSenseDto>.NotFound("Word not found.");
        }

        await RemoveCachedWordAsync(wordId, cancellationToken);

        return Result<WordSenseDto>.Ok(sense);
    }

    public async Task<Result<WordSenseDto>> UpdateSenseAsync(
        uint wordId,
        uint senseId,
        UpdateSenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var sense = await _wordRepository.UpdateSenseAsync(
            wordId,
            senseId,
            NormalizeUpdateSenseRequest(request),
            cancellationToken);
        if (sense is null)
        {
            return Result<WordSenseDto>.NotFound("Sense not found.");
        }

        await RemoveCachedWordAsync(wordId, cancellationToken);

        return Result<WordSenseDto>.Ok(sense);
    }

    public Task<Result<bool>> SoftDeleteSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Fail("Sense soft delete is not supported by current database schema."));
    }

    public Task<Result<bool>> RestoreSenseAsync(
        uint wordId,
        uint senseId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Fail("Sense restore is not supported by current database schema."));
    }

    private async Task RemoveCachedWordAsync(uint wordId, CancellationToken cancellationToken)
    {
        if (_wordDetailCache is not null)
        {
            await _wordDetailCache.RemoveAsync(wordId, cancellationToken);
        }
    }

    private static string? NormalizeCefr(string? cefr)
    {
        return string.IsNullOrWhiteSpace(cefr) ? null : cefr.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static CreateSenseRequest NormalizeCreateSenseRequest(CreateSenseRequest request)
    {
        return request with
        {
            WordClass = request.WordClass!.Trim(),
            EnglishDefinition = request.EnglishDefinition!.Trim(),
            VietnameseMeaning = NormalizeNullable(request.VietnameseMeaning),
        };
    }

    private static UpdateSenseRequest NormalizeUpdateSenseRequest(UpdateSenseRequest request)
    {
        return request with
        {
            WordClass = request.WordClass!.Trim(),
            EnglishDefinition = request.EnglishDefinition!.Trim(),
            VietnameseMeaning = NormalizeNullable(request.VietnameseMeaning),
        };
    }

    private static IReadOnlyCollection<string> RequiredCsvColumns()
    {
        return
        [
            CsvWordColumn,
            CsvCefrColumn,
            CsvPhoneticUkColumn,
            CsvPhoneticUsColumn,
            CsvWordClassColumn,
            CsvEnglishDefinitionColumn,
            CsvVietnameseMeaningColumn,
        ];
    }

    private static Dictionary<string, int> BuildHeaderIndexes(IReadOnlyList<string> headers)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index].Trim().TrimStart('\uFEFF').ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(header))
            {
                indexes[header] = index;
            }
        }

        return indexes;
    }

    private static string GetCell(
        IReadOnlyList<string> cells,
        IReadOnlyDictionary<string, int> headerIndexes,
        string column)
    {
        return headerIndexes.TryGetValue(column, out var index) && index < cells.Count
            ? cells[index].Trim()
            : string.Empty;
    }

    private static bool TryValidateImportRow(
        CsvImportRow row,
        int rowNumber,
        ICollection<BulkImportErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(row.Word))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvWordColumn, "Word is required."));
            return false;
        }

        if (row.Word.Length > 150)
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvWordColumn, "Word must be 150 characters or fewer."));
            return false;
        }

        var cefr = NormalizeCefr(row.Cefr);
        if (cefr is not null && !CefrLevel.All.Contains(cefr))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvCefrColumn, "Cefr must be one of: A1, A2, B1, B2, C1, C2."));
            return false;
        }

        if (row.PhoneticUk.Length > 100)
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvPhoneticUkColumn, "PhoneticUk must be 100 characters or fewer."));
            return false;
        }

        if (row.PhoneticUs.Length > 100)
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvPhoneticUsColumn, "PhoneticUs must be 100 characters or fewer."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(row.WordClass))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvWordClassColumn, "WordClass is required."));
            return false;
        }

        if (row.WordClass.Length > 30)
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvWordClassColumn, "WordClass must be 30 characters or fewer."));
            return false;
        }

        if (string.IsNullOrWhiteSpace(row.EnglishDefinition))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvEnglishDefinitionColumn, "EnglishDefinition is required."));
            return false;
        }

        return true;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }

    private sealed record CsvImportRow(
        string Word,
        string Cefr,
        string PhoneticUk,
        string PhoneticUs,
        string WordClass,
        string EnglishDefinition,
        string VietnameseMeaning);
}
