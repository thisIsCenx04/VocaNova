using System.Text;
using Microsoft.VisualBasic.FileIO;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Lists.BLL.Abstractions;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public sealed class WordAdminService : IWordAdminService
{
    private static readonly IReadOnlySet<string> AdminSortColumns =
        new HashSet<string>(StringComparer.Ordinal) { "word", "type", "cefr", "phonetic", "status" };
    private static readonly IReadOnlySet<string> AllowedCsvContentTypes = new HashSet<string>(
        ["text/csv", "application/csv", "application/vnd.ms-excel", "application/octet-stream"],
        StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> AllowedAudioContentTypes = new HashSet<string>(
        ["audio/mpeg", "audio/wav", "audio/ogg"], StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> AllowedImageContentTypes = new HashSet<string>(
        ["image/jpeg", "image/png", "image/webp"], StringComparer.OrdinalIgnoreCase);

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

    private readonly IWordAdminRepository _repository;
    private readonly IWordAudioStorage? _audioStorage;
    private readonly IWordImageStorage? _imageStorage;
    private readonly IWordDetailCache? _wordDetailCache;
    private readonly IUserListCache? _userListCache;

    public WordAdminService(
        IWordAdminRepository repository,
        IWordDetailCache? wordDetailCache = null,
        IWordAudioStorage? audioStorage = null,
        IWordImageStorage? imageStorage = null,
        IUserListCache? userListCache = null)
    {
        _repository = repository;
        _audioStorage = audioStorage;
        _imageStorage = imageStorage;
        _wordDetailCache = wordDetailCache;
        _userListCache = userListCache;
    }

    public async Task<DictionaryResult<PagedCollection<AdminWordListItem>>> SearchAsync(
        AdminWordQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0) return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure("Page must be greater than zero.");
        if (query.Limit <= 0 || query.Limit > AppSettings.MaxPageLimit)
            return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure($"Limit must be between 1 and {AppSettings.MaxPageLimit}.");

        var cefr = NormalizeCefr(query.Cefr);
        if (cefr is not null && !CefrLevel.All.Contains(cefr))
            return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure("Cefr must be one of: A1, A2, B1, B2, C1, C2.");
        var status = NormalizeLower(query.Status);
        if (status is not null && status is not (UserStatus.Active or UserStatus.Deleted))
            return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure("Status must be 'active' or 'deleted'.");
        var sortBy = NormalizeLower(query.SortBy);
        if (sortBy is not null && !AdminSortColumns.Contains(sortBy))
            return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure("Sort column is invalid.");
        var direction = NormalizeLower(query.SortDirection);
        if (direction is not null && direction is not ("asc" or "desc"))
            return DictionaryResult<PagedCollection<AdminWordListItem>>.ValidationFailure("Sort direction must be 'asc' or 'desc'.");

        var normalized = query with
        {
            Q = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.NormalizeWord(),
            Cefr = cefr,
            Status = status,
            IncludeDeleted = query.IncludeDeleted || status == UserStatus.Deleted,
            WordType = NormalizeLower(query.WordType),
            SortBy = sortBy,
            SortDirection = direction,
        };
        return DictionaryResult<PagedCollection<AdminWordListItem>>.Success(
            await _repository.SearchAsync(normalized, cancellationToken));
    }

    public async Task<DictionaryResult<WordDetail>> CreateAsync(
        CreateWordCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(command);
        if (await _repository.WordKeyExistsAsync(normalized.WordKey, cancellationToken: cancellationToken))
            return DictionaryResult<WordDetail>.Conflict("Word already exists.");
        return DictionaryResult<WordDetail>.Success(await _repository.CreateAsync(normalized, cancellationToken));
    }

    public async Task<DictionaryResult<WordDetail>> UpdateAsync(
        uint wordId, UpdateWordCommand command, CancellationToken cancellationToken = default)
    {
        if (wordId == 0) return DictionaryResult<WordDetail>.NotFound("Word not found.");
        var normalized = Normalize(command);
        if (await _repository.WordKeyExistsAsync(normalized.WordKey, wordId, cancellationToken))
            return DictionaryResult<WordDetail>.Conflict("Word already exists.");
        var word = await _repository.UpdateMetadataAsync(wordId, normalized, cancellationToken);
        if (word is null) return DictionaryResult<WordDetail>.NotFound("Word not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<WordDetail>.Success(word);
    }

    public async Task<DictionaryResult<BulkImportResult>> ImportCsvAsync(
        UploadedContent? content, CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length == 0)
            return DictionaryResult<BulkImportResult>.ValidationFailure("CSV file is required.");
        var validation = ValidateCsvFile(content);
        if (validation is not null) return DictionaryResult<BulkImportResult>.ValidationFailure(validation);

        var errors = new List<BulkImportError>();
        var knownWordIds = new Dictionary<string, uint>(StringComparer.Ordinal);
        var importedWords = 0; var importedSenses = 0; var updatedWords = 0;
        var importedTopics = 0; var importedExamples = 0; var skipped = 0;
        using var parser = CreateCsvParser(content.Content);
        var headers = parser.ReadFields();
        if (headers is null || headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
            return DictionaryResult<BulkImportResult>.ValidationFailure("CSV header is required.");
        var headerIndexes = BuildHeaderIndexes(headers);
        var missingColumn = RequiredCsvColumns().FirstOrDefault(column => !headerIndexes.ContainsKey(column));
        if (missingColumn is not null)
            return DictionaryResult<BulkImportResult>.ValidationFailure($"CSV header must include '{missingColumn}'.");

        var rowNumber = 1;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] cells; rowNumber++;
            try { cells = parser.ReadFields() ?? []; }
            catch (MalformedLineException exception)
            {
                errors.Add(new BulkImportError(rowNumber, "csv", exception.Message)); skipped++; continue;
            }
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            var row = new CsvImportRow(
                GetCell(cells, headerIndexes, CsvWordColumn), GetCell(cells, headerIndexes, CsvCefrColumn),
                GetCell(cells, headerIndexes, CsvPhoneticUkColumn), GetCell(cells, headerIndexes, CsvPhoneticUsColumn),
                GetCell(cells, headerIndexes, CsvWordClassColumn), GetCell(cells, headerIndexes, CsvEnglishDefinitionColumn),
                GetCell(cells, headerIndexes, CsvVietnameseMeaningColumn), GetOptionalCell(cells, headerIndexes, CsvIsPhraseColumn),
                GetOptionalCell(cells, headerIndexes, CsvTopicNamesColumn), GetOptionalCell(cells, headerIndexes, CsvExampleEnColumn),
                GetOptionalCell(cells, headerIndexes, CsvExampleViColumn), GetOptionalCell(cells, headerIndexes, CsvImageUrlColumn));
            if (!TryValidateImportRow(row, rowNumber, errors, out var isPhrase)) { skipped++; continue; }

            var topicNames = SplitMultiValue(row.TopicNames);
            IReadOnlyCollection<uint> topicIds = [];
            if (topicNames.Count > 0)
            {
                var topicMap = await _repository.FindActiveTopicIdsByNamesAsync(topicNames, cancellationToken);
                var missingTopics = topicNames.Where(name => !topicMap.ContainsKey(name)).ToArray();
                if (missingTopics.Length > 0)
                {
                    errors.Add(new BulkImportError(rowNumber, CsvTopicNamesColumn, $"Topic not found: {string.Join(", ", missingTopics)}."));
                    skipped++; continue;
                }
                topicIds = topicNames.Select(name => topicMap[name]).Distinct().ToArray();
            }

            var examples = BuildExamples(row.ExampleEn, row.ExampleVi);
            var wordKey = row.Word.NormalizeWord();
            if (!knownWordIds.TryGetValue(wordKey, out var wordId))
                wordId = await _repository.FindWordIdByKeyAsync(wordKey, cancellationToken) ?? 0;
            if (wordId == 0)
            {
                var word = await _repository.CreateWithSenseAsync(
                    new CreateWordCommand(row.Word.Trim(), wordKey, NormalizeCefr(row.Cefr),
                        NormalizeNullable(row.PhoneticUk), NormalizeNullable(row.PhoneticUs),
                        NormalizeNullable(row.ImageUrl), isPhrase ?? false, topicIds),
                    new CreateSenseCommand(1, row.WordClass.Trim(), row.EnglishDefinition.Trim(),
                        NormalizeNullable(row.VietnameseMeaning), examples), cancellationToken);
                knownWordIds[wordKey] = word.WordId; importedWords++; importedSenses++;
                importedExamples += examples.Count; importedTopics += topicIds.Count; continue;
            }

            knownWordIds[wordKey] = wordId;
            var metadataUpdated = await _repository.UpdateMissingImportMetadataAsync(wordId,
                new ImportWordMetadata(NormalizeCefr(row.Cefr), NormalizeNullable(row.PhoneticUk),
                    NormalizeNullable(row.PhoneticUs), NormalizeNullable(row.ImageUrl), isPhrase), cancellationToken);
            if (metadataUpdated is null)
            {
                errors.Add(new BulkImportError(rowNumber, CsvWordColumn, "Word not found.")); skipped++; continue;
            }
            var addedTopics = await _repository.AddTopicsAsync(wordId, topicIds, cancellationToken);
            importedTopics += addedTopics;
            if (await _repository.MatchingSenseExistsAsync(wordId, row.WordClass, row.EnglishDefinition, cancellationToken))
            {
                skipped++;
                if (metadataUpdated == true || addedTopics > 0) { updatedWords++; await RemoveCachedWordAsync(wordId, cancellationToken); }
                continue;
            }
            var sense = await _repository.CreateSenseAsync(wordId,
                new CreateSenseCommand(0, row.WordClass.Trim(), row.EnglishDefinition.Trim(),
                    NormalizeNullable(row.VietnameseMeaning), examples), cancellationToken);
            if (sense is null)
            {
                errors.Add(new BulkImportError(rowNumber, CsvWordColumn, "Word not found.")); skipped++; continue;
            }
            await RemoveCachedWordAsync(wordId, cancellationToken);
            importedSenses++; importedExamples += examples.Count; updatedWords++;
        }
        return DictionaryResult<BulkImportResult>.Success(new BulkImportResult(
            importedWords, importedSenses, skipped, errors, updatedWords, importedTopics, importedExamples));
    }

    public Task<DictionaryResult<bool>> SoftDeleteAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SetWordStatusAsync(wordId, UserStatus.Deleted, cancellationToken);
    public Task<DictionaryResult<bool>> RestoreAsync(uint wordId, CancellationToken cancellationToken = default) =>
        SetWordStatusAsync(wordId, UserStatus.Active, cancellationToken);

    public async Task<DictionaryResult<WordDetail>> UploadImageAsync(
        uint wordId, UploadedContent? content, CancellationToken cancellationToken = default)
    {
        if (wordId == 0) return DictionaryResult<WordDetail>.NotFound("Word not found.");
        var validation = ValidateImageFile(content);
        if (validation is not null) return DictionaryResult<WordDetail>.ValidationFailure(validation);
        if (_imageStorage is null) return DictionaryResult<WordDetail>.ValidationFailure("Image storage is not configured.");
        if (!await _repository.WordExistsAsync(wordId, cancellationToken: cancellationToken))
            return DictionaryResult<WordDetail>.NotFound("Word not found.");
        StoredMedia uploaded;
        try { uploaded = await _imageStorage.UploadAsync(content! with { OwnerId = wordId }, cancellationToken); }
        catch (InvalidOperationException exception) { return DictionaryResult<WordDetail>.ValidationFailure(exception.Message); }
        return await SetImageUrlAsync(wordId, uploaded.Url, cancellationToken);
    }

    public Task<DictionaryResult<WordDetail>> UpdateImageUrlAsync(
        uint wordId, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (wordId == 0) return Task.FromResult(DictionaryResult<WordDetail>.NotFound("Word not found."));
        var normalized = NormalizeNullable(imageUrl);
        if (normalized is not null && !IsValidHttpsUrl(normalized))
            return Task.FromResult(DictionaryResult<WordDetail>.ValidationFailure("ImageUrl must be a valid HTTPS URL."));
        return SetImageUrlAsync(wordId, normalized, cancellationToken);
    }

    public async Task<DictionaryResult<WordAudio>> UploadAudioAsync(
        uint wordId, string? accent, UploadedContent? content, CancellationToken cancellationToken = default)
    {
        if (wordId == 0) return DictionaryResult<WordAudio>.NotFound("Word not found.");
        var normalizedAccent = NormalizeAccent(accent);
        if (normalizedAccent is null) return DictionaryResult<WordAudio>.ValidationFailure("Accent must be one of: uk, us.");
        var validation = ValidateAudioFile(content);
        if (validation is not null) return DictionaryResult<WordAudio>.ValidationFailure(validation);
        if (_audioStorage is null) return DictionaryResult<WordAudio>.ValidationFailure("Audio storage is not configured.");
        if (!await _repository.WordExistsAsync(wordId, cancellationToken: cancellationToken))
            return DictionaryResult<WordAudio>.NotFound("Word not found.");
        StoredMedia uploaded;
        try { uploaded = await _audioStorage.UploadAsync(content! with { OwnerId = wordId }, normalizedAccent, cancellationToken); }
        catch (InvalidOperationException exception) { return DictionaryResult<WordAudio>.ValidationFailure(exception.Message); }
        var audio = await _repository.UpsertAudioAsync(wordId, uploaded, normalizedAccent, cancellationToken);
        if (audio is null) return DictionaryResult<WordAudio>.NotFound("Word not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<WordAudio>.Success(audio);
    }

    public async Task<DictionaryResult<bool>> SoftDeleteAudioAsync(
        uint wordId, uint audioId, CancellationToken cancellationToken = default)
    {
        if (wordId == 0 || audioId == 0) return DictionaryResult<bool>.NotFound("Audio asset not found.");
        if (!await _repository.SetAudioStatusAsync(wordId, audioId, AudioStatus.Deleted, cancellationToken))
            return DictionaryResult<bool>.NotFound("Audio asset not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<bool>.Success(true);
    }

    public async Task<DictionaryResult<WordSense>> CreateSenseAsync(
        uint wordId, CreateSenseCommand command, CancellationToken cancellationToken = default)
    {
        var sense = await _repository.CreateSenseAsync(wordId, Normalize(command), cancellationToken);
        if (sense is null) return DictionaryResult<WordSense>.NotFound("Word not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<WordSense>.Success(sense);
    }

    public async Task<DictionaryResult<WordSense>> UpdateSenseAsync(
        uint wordId, uint senseId, UpdateSenseCommand command, CancellationToken cancellationToken = default)
    {
        var sense = await _repository.UpdateSenseAsync(wordId, senseId, Normalize(command), cancellationToken);
        if (sense is null) return DictionaryResult<WordSense>.NotFound("Sense not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<WordSense>.Success(sense);
    }

    public Task<DictionaryResult<bool>> SoftDeleteSenseAsync(uint wordId, uint senseId, CancellationToken cancellationToken = default) =>
        SetSenseStatusAsync(wordId, senseId, UserStatus.Deleted, cancellationToken);
    public Task<DictionaryResult<bool>> RestoreSenseAsync(uint wordId, uint senseId, CancellationToken cancellationToken = default) =>
        SetSenseStatusAsync(wordId, senseId, UserStatus.Active, cancellationToken);

    private async Task<DictionaryResult<bool>> SetSenseStatusAsync(
        uint wordId, uint senseId, string status, CancellationToken cancellationToken)
    {
        if (wordId == 0 || senseId == 0
            || !await _repository.SetSenseStatusAsync(wordId, senseId, status, cancellationToken))
            return DictionaryResult<bool>.NotFound("Sense not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<bool>.Success(true);
    }

    private async Task<DictionaryResult<bool>> SetWordStatusAsync(uint wordId, string status, CancellationToken cancellationToken)
    {
        if (wordId == 0 || !await _repository.SetWordStatusAsync(wordId, status, cancellationToken))
            return DictionaryResult<bool>.NotFound("Word not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        if (_userListCache is not null)
            foreach (var userId in await _repository.GetReferencingUserIdsAsync(wordId, cancellationToken))
                await _userListCache.RemoveAsync(userId, cancellationToken);
        return DictionaryResult<bool>.Success(true);
    }

    private async Task<DictionaryResult<WordDetail>> SetImageUrlAsync(uint wordId, string? url, CancellationToken cancellationToken)
    {
        var word = await _repository.SetImageUrlAsync(wordId, url, cancellationToken);
        if (word is null) return DictionaryResult<WordDetail>.NotFound("Word not found.");
        await RemoveCachedWordAsync(wordId, cancellationToken);
        return DictionaryResult<WordDetail>.Success(word);
    }

    private async Task RemoveCachedWordAsync(uint wordId, CancellationToken cancellationToken)
    {
        if (_wordDetailCache is not null) await _wordDetailCache.RemoveAsync(wordId, cancellationToken);
    }

    private static CreateWordCommand Normalize(CreateWordCommand command) => command with
    {
        Word = command.Word.Trim(), WordKey = command.Word.NormalizeWord(), Cefr = NormalizeCefr(command.Cefr),
        PhoneticUk = NormalizeNullable(command.PhoneticUk), PhoneticUs = NormalizeNullable(command.PhoneticUs),
        ImageUrl = NormalizeNullable(command.ImageUrl),
    };
    private static UpdateWordCommand Normalize(UpdateWordCommand command) => command with
    {
        Word = command.Word.Trim(), WordKey = command.Word.NormalizeWord(), Cefr = NormalizeCefr(command.Cefr),
        PhoneticUk = NormalizeNullable(command.PhoneticUk), PhoneticUs = NormalizeNullable(command.PhoneticUs),
        ImageUrl = NormalizeNullable(command.ImageUrl),
    };
    private static CreateSenseCommand Normalize(CreateSenseCommand command) => command with
    {
        WordClass = command.WordClass.Trim(), EnglishDefinition = command.EnglishDefinition.Trim(),
        VietnameseMeaning = NormalizeNullable(command.VietnameseMeaning), Examples = NormalizeExamples(command.Examples),
    };
    private static UpdateSenseCommand Normalize(UpdateSenseCommand command) => command with
    {
        WordClass = command.WordClass.Trim(), EnglishDefinition = command.EnglishDefinition.Trim(),
        VietnameseMeaning = NormalizeNullable(command.VietnameseMeaning), Examples = NormalizeExamples(command.Examples),
    };
    private static IReadOnlyList<SenseExampleInput>? NormalizeExamples(IReadOnlyList<SenseExampleInput>? examples) =>
        examples?.Select(example => example with
        {
            ExampleEn = example.ExampleEn.Trim(), ExampleVi = NormalizeNullable(example.ExampleVi),
        }).ToArray();

    private static string? ValidateCsvFile(UploadedContent content)
    {
        if (content.Length > MaxCsvFileBytes) return "CSV file must be 10MB or smaller.";
        if (!string.Equals(Path.GetExtension(content.FileName), ".csv", StringComparison.OrdinalIgnoreCase)) return "File extension must be .csv.";
        return !string.IsNullOrWhiteSpace(content.ContentType) && !AllowedCsvContentTypes.Contains(content.ContentType)
            ? "CSV MIME type must be text/csv." : null;
    }
    private static string? ValidateAudioFile(UploadedContent? content) => content switch
    {
        null or { Length: 0 } => "Audio file is required.",
        { Length: > MaxAudioFileBytes } => "Audio file must be 5MB or smaller.",
        _ when !AllowedAudioContentTypes.Contains(content.ContentType) => "Audio MIME type must be one of: audio/mpeg, audio/wav, audio/ogg.",
        _ => null,
    };
    private static string? ValidateImageFile(UploadedContent? content) => content switch
    {
        null or { Length: 0 } => "Image file is required.",
        { Length: > MaxImageFileBytes } => "Image file must be 5MB or smaller.",
        _ when !AllowedImageContentTypes.Contains(content.ContentType) => "Image MIME type must be one of: image/jpeg, image/png, image/webp.",
        _ => null,
    };
    private static string? NormalizeCefr(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeLower(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static string? NormalizeAccent(string? value) { var normalized = NormalizeLower(value); return normalized is not null && AudioAccent.All.Contains(normalized) ? normalized : null; }
    private static bool IsValidHttpsUrl(string value) => value.Length <= 500 && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static bool IsValidImportUrl(string value) => value.Length <= 500 && Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    private static TextFieldParser CreateCsvParser(Stream stream) { var parser = new TextFieldParser(stream, Encoding.UTF8, true) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false }; parser.SetDelimiters(","); return parser; }
    private static IReadOnlyCollection<string> RequiredCsvColumns() => [CsvWordColumn, CsvCefrColumn, CsvPhoneticUkColumn, CsvPhoneticUsColumn, CsvWordClassColumn, CsvEnglishDefinitionColumn, CsvVietnameseMeaningColumn];
    private static Dictionary<string, int> BuildHeaderIndexes(IReadOnlyList<string> headers) { var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); for (var i = 0; i < headers.Count; i++) { var header = headers[i].Trim().TrimStart('\uFEFF').ToLowerInvariant(); if (header.Length > 0) indexes[header] = i; } return indexes; }
    private static string GetCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> indexes, string column) => indexes.TryGetValue(column, out var i) && i < cells.Count ? cells[i].Trim() : string.Empty;
    private static string? GetOptionalCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> indexes, string column) => indexes.ContainsKey(column) ? GetCell(cells, indexes, column) : null;
    private static IReadOnlyList<string> SplitMultiValue(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(['|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(item => item.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static IReadOnlyList<SenseExampleInput> BuildExamples(string? en, string? vi) { var ens = SplitMultiValue(en); var vis = SplitMultiValue(vi); return ens.Select((value, i) => new SenseExampleInput(null, value, i < vis.Count ? vis[i] : null)).ToArray(); }
    private static bool TryParseBool(string value, out bool result) { switch (value.Trim().ToLowerInvariant()) { case "true": case "1": case "yes": case "y": result = true; return true; case "false": case "0": case "no": case "n": result = false; return true; default: result = false; return false; } }
    private static bool TryValidateImportRow(CsvImportRow row, int number, ICollection<BulkImportError> errors, out bool? isPhrase)
    {
        isPhrase = null;
        if (string.IsNullOrWhiteSpace(row.Word)) { errors.Add(new(number, CsvWordColumn, "Word is required.")); return false; }
        if (row.Word.Length > 150) { errors.Add(new(number, CsvWordColumn, "Word must be 150 characters or fewer.")); return false; }
        var cefr = NormalizeCefr(row.Cefr); if (cefr is not null && !CefrLevel.All.Contains(cefr)) { errors.Add(new(number, CsvCefrColumn, "Cefr must be one of: A1, A2, B1, B2, C1, C2.")); return false; }
        if (row.PhoneticUk.Length > 100) { errors.Add(new(number, CsvPhoneticUkColumn, "PhoneticUk must be 100 characters or fewer.")); return false; }
        if (row.PhoneticUs.Length > 100) { errors.Add(new(number, CsvPhoneticUsColumn, "PhoneticUs must be 100 characters or fewer.")); return false; }
        if (string.IsNullOrWhiteSpace(row.WordClass)) { errors.Add(new(number, CsvWordClassColumn, "WordClass is required.")); return false; }
        if (row.WordClass.Length > 30) { errors.Add(new(number, CsvWordClassColumn, "WordClass must be 30 characters or fewer.")); return false; }
        if (string.IsNullOrWhiteSpace(row.EnglishDefinition)) { errors.Add(new(number, CsvEnglishDefinitionColumn, "EnglishDefinition is required.")); return false; }
        if (!string.IsNullOrWhiteSpace(row.ImageUrl) && !IsValidImportUrl(row.ImageUrl)) { errors.Add(new(number, CsvImageUrlColumn, "ImageUrl must be a valid absolute URL.")); return false; }
        if (!string.IsNullOrWhiteSpace(row.IsPhrase))
        {
            if (!TryParseBool(row.IsPhrase, out var parsedIsPhrase))
            {
                errors.Add(new(number, CsvIsPhraseColumn, "IsPhrase must be true/false, yes/no, or 1/0."));
                return false;
            }
            isPhrase = parsedIsPhrase;
        }
        return true;
    }

    private sealed record CsvImportRow(string Word, string Cefr, string PhoneticUk, string PhoneticUs,
        string WordClass, string EnglishDefinition, string VietnameseMeaning, string? IsPhrase,
        string? TopicNames, string? ExampleEn, string? ExampleVi, string? ImageUrl);
}
