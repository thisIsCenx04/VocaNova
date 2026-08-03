using System.Text;
using Microsoft.VisualBasic.FileIO;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Features.Dictionary.Repositories;
using VocaNova.API.Infrastructure.Caching;
using VocaNova.API.Infrastructure.Storage;

namespace VocaNova.API.Features.Dictionary.Services;

public sealed class WordService : IWordService
{
    private static readonly IReadOnlySet<string> AdminSortColumns =
        new HashSet<string>(StringComparer.Ordinal) { "word", "type", "cefr", "phonetic", "status" };

    private const string CsvWordColumn = "word";
    private const string CsvCefrColumn = "cefr_level";
    private const string CsvPhoneticUkColumn = "phonetic_uk";
    private const string CsvPhoneticUsColumn = "phonetic_us";
    private const string CsvWordClassColumn = "word_class";
    private const string CsvEnglishDefinitionColumn = "english_definition";
    private const string CsvVietnameseMeaningColumn = "vietnamese_meaning";
    private const string CsvIsPhraseColumn = "is_phrase";
    private const string CsvTopicNamesColumn = "topic_names";
    private const string CsvExampleEnColumn = "example_en";
    private const string CsvExampleViColumn = "example_vi";
    private const string CsvImageUrlColumn = "image_url";
    private const long MaxCsvFileBytes = 10 * 1024 * 1024;
    private const long MaxAudioFileBytes = 5 * 1024 * 1024;
    private const long MaxImageFileBytes = 5 * 1024 * 1024;

    private readonly IWordRepository _wordRepository;
    private readonly IAudioStorage? _audioStorage;
    private readonly IImageStorage? _imageStorage;
    private readonly IWordSearchCache? _wordSearchCache;
    private readonly IWordDetailCache? _wordDetailCache;

    public WordService(
        IWordRepository wordRepository,
        IWordSearchCache? wordSearchCache = null,
        IWordDetailCache? wordDetailCache = null,
        IAudioStorage? audioStorage = null,
        IImageStorage? imageStorage = null)
    {
        _wordRepository = wordRepository;
        _audioStorage = audioStorage;
        _imageStorage = imageStorage;
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

    public async Task<Result<PagedResult<AdminWordListItemDto>>> SearchAdminAsync(
        AdminWordQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0)
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail("Page must be greater than zero.");
        }

        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");
        }

        var cefr = string.IsNullOrWhiteSpace(query.Cefr)
            ? null
            : query.Cefr.Trim().ToUpperInvariant();
        if (cefr is not null && !CefrLevel.All.Contains(cefr))
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail("Cefr must be one of: A1, A2, B1, B2, C1, C2.");
        }

        var status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToLowerInvariant();
        if (status is not null && status != UserStatus.Active && status != UserStatus.Deleted)
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail("Status must be 'active' or 'deleted'.");
        }

        // Lọc status='deleted' chỉ có ý nghĩa khi đã bỏ global filter.
        var includeDeleted = query.IncludeDeleted || status == UserStatus.Deleted;

        var normalizedQuery = string.IsNullOrWhiteSpace(query.Q)
            ? null
            : query.Q.NormalizeWord();

        var wordType = string.IsNullOrWhiteSpace(query.WordType)
            ? null
            : query.WordType.Trim().ToLowerInvariant();

        var sortBy = string.IsNullOrWhiteSpace(query.SortBy)
            ? null
            : query.SortBy.Trim().ToLowerInvariant();
        if (sortBy is not null && !AdminSortColumns.Contains(sortBy))
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail("Sort column is invalid.");
        }

        var sortDirection = string.IsNullOrWhiteSpace(query.SortDirection)
            ? null
            : query.SortDirection.Trim().ToLowerInvariant();
        if (sortDirection is not null && sortDirection is not ("asc" or "desc"))
        {
            return Result<PagedResult<AdminWordListItemDto>>.Fail("Sort direction must be 'asc' or 'desc'.");
        }

        var result = await _wordRepository.SearchAdminAsync(
            normalizedQuery,
            query.Page,
            query.Limit,
            cefr,
            query.TopicId,
            status,
            includeDeleted,
            wordType,
            sortBy,
            sortDirection,
            cancellationToken);

        return Result<PagedResult<AdminWordListItemDto>>.Ok(result);
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

    public async Task<Result<WordDetailDto>> GetDailyAsync(
        CancellationToken cancellationToken = default)
    {
        var word = await _wordRepository.FindDailyDetailAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return word is null
            ? Result<WordDetailDto>.NotFound("No daily word is available.")
            : Result<WordDetailDto>.Ok(word);
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
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return Result<BulkImportResultDto>.Fail("CSV file is required.");
        }

        var fileValidation = ValidateCsvFile(file);
        if (!fileValidation.IsSuccess)
        {
            return Result<BulkImportResultDto>.Fail(fileValidation.Error!);
        }

        var errors = new List<BulkImportErrorDto>();
        var knownWordIds = new Dictionary<string, uint>(StringComparer.Ordinal);
        var importedWords = 0;
        var importedSenses = 0;
        var updatedWords = 0;
        var importedTopics = 0;
        var importedExamples = 0;
        var skipped = 0;

        await using var stream = file.OpenReadStream();
        using var parser = CreateCsvParser(stream);
        var headers = parser.ReadFields();
        if (headers is null || headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            return Result<BulkImportResultDto>.Fail("CSV header is required.");
        }

        var headerIndexes = BuildHeaderIndexes(headers);
        var missingColumn = RequiredCsvColumns()
            .FirstOrDefault(column => !headerIndexes.ContainsKey(column));
        if (missingColumn is not null)
        {
            return Result<BulkImportResultDto>.Fail($"CSV header must include '{missingColumn}'.");
        }

        var rowNumber = 1;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[] cells;
            rowNumber++;
            try
            {
                cells = parser.ReadFields() ?? Array.Empty<string>();
            }
            catch (MalformedLineException exception)
            {
                errors.Add(new BulkImportErrorDto(rowNumber, "csv", exception.Message));
                skipped++;
                continue;
            }

            if (cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var row = new CsvImportRow(
                GetCell(cells, headerIndexes, CsvWordColumn),
                GetCell(cells, headerIndexes, CsvCefrColumn),
                GetCell(cells, headerIndexes, CsvPhoneticUkColumn),
                GetCell(cells, headerIndexes, CsvPhoneticUsColumn),
                GetCell(cells, headerIndexes, CsvWordClassColumn),
                GetCell(cells, headerIndexes, CsvEnglishDefinitionColumn),
                GetCell(cells, headerIndexes, CsvVietnameseMeaningColumn),
                GetOptionalCell(cells, headerIndexes, CsvIsPhraseColumn),
                GetOptionalCell(cells, headerIndexes, CsvTopicNamesColumn),
                GetOptionalCell(cells, headerIndexes, CsvExampleEnColumn),
                GetOptionalCell(cells, headerIndexes, CsvExampleViColumn),
                GetOptionalCell(cells, headerIndexes, CsvImageUrlColumn));

            if (!TryValidateImportRow(row, rowNumber, errors, out var isPhrase))
            {
                skipped++;
                continue;
            }

            var topicNames = SplitMultiValue(row.TopicNames);
            var topicIds = Array.Empty<uint>();
            if (topicNames.Count > 0)
            {
                var topicMap = await _wordRepository.FindActiveTopicIdsByNamesAsync(topicNames, cancellationToken);
                var missingTopics = topicNames
                    .Where(name => !topicMap.ContainsKey(name))
                    .ToArray();
                if (missingTopics.Length > 0)
                {
                    errors.Add(new BulkImportErrorDto(
                        rowNumber,
                        CsvTopicNamesColumn,
                        $"Topic not found: {string.Join(", ", missingTopics)}."));
                    skipped++;
                    continue;
                }

                topicIds = topicNames.Select(name => topicMap[name]).Distinct().ToArray();
            }

            var examples = BuildExamples(row.ExampleEn, row.ExampleVi);

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
                        NormalizeNullable(row.ImageUrl),
                        isPhrase ?? false),
                    wordKey,
                    new CreateSenseRequest(
                        1,
                        row.WordClass.Trim(),
                        row.EnglishDefinition.Trim(),
                        NormalizeNullable(row.VietnameseMeaning)),
                    examples,
                    topicIds,
                    cancellationToken);

                knownWordIds[wordKey] = word.WordId;
                importedWords++;
                importedSenses++;
                importedExamples += examples.Count;
                importedTopics += topicIds.Length;
                continue;
            }

            knownWordIds[wordKey] = wordId;

            var metadataUpdated = await _wordRepository.UpdateMissingImportMetadataAsync(
                wordId,
                NormalizeCefr(row.Cefr),
                NormalizeNullable(row.PhoneticUk),
                NormalizeNullable(row.PhoneticUs),
                NormalizeNullable(row.ImageUrl),
                isPhrase,
                cancellationToken);
            if (metadataUpdated is null)
            {
                errors.Add(new BulkImportErrorDto(rowNumber, CsvWordColumn, "Word not found."));
                skipped++;
                continue;
            }

            var addedTopics = await _wordRepository.AddTopicsToWordAsync(wordId, topicIds, cancellationToken);
            importedTopics += addedTopics;

            if (await _wordRepository.SenseExistsAsync(
                    wordId,
                    row.WordClass,
                    row.EnglishDefinition,
                    cancellationToken))
            {
                skipped++;
                if (metadataUpdated == true || addedTopics > 0)
                {
                    updatedWords++;
                    await RemoveCachedWordAsync(wordId, cancellationToken);
                }

                continue;
            }

            var sense = await _wordRepository.CreateNextSenseAsync(
                wordId,
                row.WordClass,
                row.EnglishDefinition,
                row.VietnameseMeaning,
                examples,
                cancellationToken);
            if (sense is null)
            {
                errors.Add(new BulkImportErrorDto(rowNumber, CsvWordColumn, "Word not found."));
                skipped++;
                continue;
            }

            await RemoveCachedWordAsync(wordId, cancellationToken);
            importedSenses++;
            importedExamples += examples.Count;
            updatedWords++;
        }

        return Result<BulkImportResultDto>.Ok(new BulkImportResultDto(
            importedWords,
            importedSenses,
            skipped,
            errors,
            updatedWords,
            importedTopics,
            importedExamples));
    }

    public async Task<Result<bool>> SoftDeleteAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        // Notifications for affected users are derived on read from the word's soft-deleted state
        // (see NotificationRepository), so no notification write is needed here.
        return await SetWordStatusAsync(wordId, UserStatus.Deleted, cancellationToken);
    }

    public async Task<Result<bool>> RestoreAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return await SetWordStatusAsync(wordId, UserStatus.Active, cancellationToken);
    }

    public async Task<Result<WordDetailDto>> UploadImageAsync(
        uint wordId,
        UploadWordImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        var fileValidation = ValidateImageFile(request.File);
        if (!fileValidation.IsSuccess)
        {
            return Result<WordDetailDto>.Fail(fileValidation.Error!);
        }

        if (_imageStorage is null)
        {
            return Result<WordDetailDto>.Fail("Image storage is not configured.");
        }

        if (!await _wordRepository.WordExistsAsync(wordId, cancellationToken))
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        ImageStorageResult uploadResult;
        try
        {
            uploadResult = await _imageStorage.UploadAsync(
                wordId,
                request.File!,
                cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Result<WordDetailDto>.Fail(exception.Message);
        }

        return await SetImageUrlAsync(wordId, uploadResult.Url, cancellationToken);
    }

    public Task<Result<WordDetailDto>> UpdateImageUrlAsync(
        uint wordId,
        UpdateWordImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Task.FromResult(Result<WordDetailDto>.NotFound("Word not found."));
        }

        var normalizedUrl = NormalizeNullable(request.ImageUrl);
        if (normalizedUrl is not null && !IsValidHttpsUrl(normalizedUrl))
        {
            return Task.FromResult(Result<WordDetailDto>.Fail("ImageUrl must be a valid HTTPS URL."));
        }

        return SetImageUrlAsync(wordId, normalizedUrl, cancellationToken);
    }

    public async Task<Result<WordAudioDto>> UploadAudioAsync(
        uint wordId,
        UploadWordAudioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0)
        {
            return Result<WordAudioDto>.NotFound("Word not found.");
        }

        var accent = NormalizeAccent(request.Accent);
        if (accent is null)
        {
            return Result<WordAudioDto>.Fail("Accent must be one of: uk, us.");
        }

        var fileValidation = ValidateAudioFile(request.File);
        if (!fileValidation.IsSuccess)
        {
            return Result<WordAudioDto>.Fail(fileValidation.Error!);
        }

        if (_audioStorage is null)
        {
            return Result<WordAudioDto>.Fail("Audio storage is not configured.");
        }

        if (!await _wordRepository.WordExistsAsync(wordId, cancellationToken))
        {
            return Result<WordAudioDto>.NotFound("Word not found.");
        }

        AudioStorageResult uploadResult;
        try
        {
            uploadResult = await _audioStorage.UploadAsync(wordId, accent, request.File!, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Result<WordAudioDto>.Fail(exception.Message);
        }

        var audio = await _wordRepository.UpsertAudioAsync(
            wordId,
            accent,
            uploadResult.Url,
            cancellationToken);
        if (audio is null)
        {
            return Result<WordAudioDto>.NotFound("Word not found.");
        }

        await RemoveCachedWordAsync(wordId, cancellationToken);
        return Result<WordAudioDto>.Ok(audio);
    }

    public async Task<Result<bool>> SoftDeleteAudioAsync(
        uint wordId,
        uint audioId,
        CancellationToken cancellationToken = default)
    {
        if (wordId == 0 || audioId == 0)
        {
            return Result<bool>.NotFound("Audio asset not found.");
        }

        var updated = await _wordRepository.SetAudioStatusAsync(
            wordId,
            audioId,
            AudioStatus.Deleted,
            cancellationToken);
        if (!updated)
        {
            return Result<bool>.NotFound("Audio asset not found.");
        }

        await RemoveCachedWordAsync(wordId, cancellationToken);
        return Result<bool>.Ok(true);
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

    private static string? NormalizeAccent(string? accent)
    {
        if (string.IsNullOrWhiteSpace(accent))
        {
            return null;
        }

        var normalized = accent.Trim().ToLowerInvariant();
        return AudioAccent.All.Contains(normalized) ? normalized : null;
    }

    private static Result<bool> ValidateAudioFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return Result<bool>.Fail("Audio file is required.");
        }

        if (file.Length > MaxAudioFileBytes)
        {
            return Result<bool>.Fail("Audio file must be 5MB or smaller.");
        }

        if (!AllowedAudioContentTypes.Contains(file.ContentType))
        {
            return Result<bool>.Fail("Audio MIME type must be one of: audio/mpeg, audio/wav, audio/ogg.");
        }

        return Result<bool>.Ok(true);
    }

    private static readonly IReadOnlySet<string> AllowedAudioContentTypes = new HashSet<string>(
        new[] { "audio/mpeg", "audio/wav", "audio/ogg" },
        StringComparer.OrdinalIgnoreCase);

    private static Result<bool> ValidateCsvFile(IFormFile file)
    {
        if (file.Length > MaxCsvFileBytes)
        {
            return Result<bool>.Fail("CSV file must be 10MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Fail("File extension must be .csv.");
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !AllowedCsvContentTypes.Contains(file.ContentType))
        {
            return Result<bool>.Fail("CSV MIME type must be text/csv.");
        }

        return Result<bool>.Ok(true);
    }

    private static readonly IReadOnlySet<string> AllowedCsvContentTypes = new HashSet<string>(
        new[] { "text/csv", "application/csv", "application/vnd.ms-excel", "application/octet-stream" },
        StringComparer.OrdinalIgnoreCase);

    private async Task<Result<WordDetailDto>> SetImageUrlAsync(
        uint wordId,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        var word = await _wordRepository.SetImageUrlAsync(wordId, imageUrl, cancellationToken);
        if (word is null)
        {
            return Result<WordDetailDto>.NotFound("Word not found.");
        }

        await RemoveCachedWordAsync(wordId, cancellationToken);
        return Result<WordDetailDto>.Ok(word);
    }

    private static Result<bool> ValidateImageFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return Result<bool>.Fail("Image file is required.");
        }

        if (file.Length > MaxImageFileBytes)
        {
            return Result<bool>.Fail("Image file must be 5MB or smaller.");
        }

        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            return Result<bool>.Fail("Image MIME type must be one of: image/jpeg, image/png, image/webp.");
        }

        return Result<bool>.Ok(true);
    }

    private static bool IsValidHttpsUrl(string? imageUrl)
    {
        return !string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.Length <= 500
            && Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static readonly IReadOnlySet<string> AllowedImageContentTypes = new HashSet<string>(
        new[] { "image/jpeg", "image/png", "image/webp" },
        StringComparer.OrdinalIgnoreCase);

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

    private static string? GetOptionalCell(
        IReadOnlyList<string> cells,
        IReadOnlyDictionary<string, int> headerIndexes,
        string column)
    {
        return headerIndexes.ContainsKey(column)
            ? GetCell(cells, headerIndexes, column)
            : null;
    }

    private static bool TryValidateImportRow(
        CsvImportRow row,
        int rowNumber,
        ICollection<BulkImportErrorDto> errors,
        out bool? isPhrase)
    {
        isPhrase = null;
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

        if (!string.IsNullOrWhiteSpace(row.ImageUrl) && !IsValidImportUrl(row.ImageUrl))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvImageUrlColumn, "ImageUrl must be a valid absolute URL."));
            return false;
        }

        bool parsedIsPhrase = false;
        if (!string.IsNullOrWhiteSpace(row.IsPhrase) && !TryParseBool(row.IsPhrase, out parsedIsPhrase))
        {
            errors.Add(new BulkImportErrorDto(rowNumber, CsvIsPhraseColumn, "IsPhrase must be true/false, yes/no, or 1/0."));
            return false;
        }

        isPhrase = string.IsNullOrWhiteSpace(row.IsPhrase) ? null : parsedIsPhrase;
        return true;
    }

    private static TextFieldParser CreateCsvParser(Stream stream)
    {
        var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };
        parser.SetDelimiters(",");
        return parser;
    }

    private static IReadOnlyList<string> SplitMultiValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value
                .Split(['|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static IReadOnlyList<SenseExampleInput> BuildExamples(string? exampleEn, string? exampleVi)
    {
        var englishExamples = SplitMultiValue(exampleEn);
        if (englishExamples.Count == 0)
        {
            return Array.Empty<SenseExampleInput>();
        }

        var vietnameseExamples = SplitMultiValue(exampleVi);
        return englishExamples
            .Select((english, index) => new SenseExampleInput(
                null,
                english,
                index < vietnameseExamples.Count ? vietnameseExamples[index] : null))
            .ToArray();
    }

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                result = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static bool IsValidImportUrl(string value)
    {
        return value.Length <= 500
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private sealed record CsvImportRow(
        string Word,
        string Cefr,
        string PhoneticUk,
        string PhoneticUs,
        string WordClass,
        string EnglishDefinition,
        string VietnameseMeaning,
        string? IsPhrase,
        string? TopicNames,
        string? ExampleEn,
        string? ExampleVi,
        string? ImageUrl);
}
