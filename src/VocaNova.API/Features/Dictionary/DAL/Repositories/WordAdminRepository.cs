using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;
using AdminWordAudio = VocaNova.API.Features.Dictionary.BLL.Models.WordAudio;
using AdminWordSense = VocaNova.API.Features.Dictionary.BLL.Models.WordSense;
using PersistenceWordExample = VocaNova.API.Infrastructure.Persistence.Entities.WordExample;
using PersistenceWordTopic = VocaNova.API.Infrastructure.Persistence.Entities.WordTopic;

namespace VocaNova.API.Features.Dictionary.DAL.Repositories;

public sealed class WordAdminRepository : IWordAdminRepository
{
    private readonly VocaNovaDbContext _dbContext;
    public WordAdminRepository(VocaNovaDbContext dbContext) => _dbContext = dbContext;

    public async Task<PagedCollection<AdminWordListItem>> SearchAsync(
        AdminWordQuery filter, CancellationToken cancellationToken = default)
    {
        var query = filter.IncludeDeleted
            ? _dbContext.Words.IgnoreQueryFilters().AsNoTracking()
            : _dbContext.Words.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Q)) query = query.Where(word => EF.Functions.Like(word.WordKey, filter.Q + "%"));
        if (!string.IsNullOrWhiteSpace(filter.Cefr)) query = query.Where(word => word.CefrLevel == filter.Cefr);
        if (filter.TopicId.HasValue) query = query.Where(word => word.WordTopics.Any(link => link.TopicId == filter.TopicId));
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(word => word.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.WordType)) query = query.Where(word => word.WordSenses.Any(sense => sense.WordClass == filter.WordType));

        IOrderedQueryable<Word> ordered = (filter.SortBy, filter.SortDirection) switch
        {
            ("word", "desc") => query.OrderByDescending(word => word.WordKey),
            ("type", "desc") => query.OrderByDescending(word => word.WordSenses.OrderBy(sense => sense.SenseOrder).Select(sense => sense.WordClass).FirstOrDefault()),
            ("type", _) => query.OrderBy(word => word.WordSenses.OrderBy(sense => sense.SenseOrder).Select(sense => sense.WordClass).FirstOrDefault()),
            ("cefr", "desc") => query.OrderByDescending(word => word.CefrLevel),
            ("cefr", _) => query.OrderBy(word => word.CefrLevel),
            ("phonetic", "desc") => query.OrderByDescending(word => word.PhoneticUs ?? word.PhoneticUk),
            ("phonetic", _) => query.OrderBy(word => word.PhoneticUs ?? word.PhoneticUk),
            ("status", "desc") => query.OrderByDescending(word => word.Status),
            ("status", _) => query.OrderBy(word => word.Status),
            _ => query.OrderBy(word => word.WordKey),
        };
        ordered = ordered.ThenBy(word => word.WordId);
        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered.Skip((filter.Page - 1) * filter.Limit).Take(filter.Limit)
            .Select(word => new AdminWordListItem(
                word.WordId, word.Word1, word.CefrLevel, word.PhoneticUs ?? word.PhoneticUk,
                word.Status, word.ImageUrl,
                word.WordSenses.Where(sense => filter.WordType == null || sense.WordClass == filter.WordType)
                    .OrderBy(sense => sense.SenseOrder).Select(sense => sense.VietnameseMeaning).FirstOrDefault(),
                word.WordTopics.OrderBy(link => link.Topic.TopicName)
                    .Select(link => new VocaNova.API.Features.Dictionary.BLL.Models.WordTopic(
                        link.TopicId, link.Topic.TopicName, link.Topic.TopicNameVi, link.Topic.Icon)).ToList(),
                word.WordSenses.Where(sense => filter.WordType == null || sense.WordClass == filter.WordType)
                    .OrderBy(sense => sense.SenseOrder).Select(sense => sense.WordClass).FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new PagedCollection<AdminWordListItem>(items, filter.Page, filter.Limit, total);
    }

    public Task<bool> WordKeyExistsAsync(string wordKey, uint? excludingId = null, CancellationToken cancellationToken = default) =>
        _dbContext.Words.IgnoreQueryFilters().AnyAsync(word => word.WordKey == wordKey
            && (!excludingId.HasValue || word.WordId != excludingId), cancellationToken);

    public Task<bool> WordExistsAsync(uint wordId, bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        (includeDeleted ? _dbContext.Words.IgnoreQueryFilters() : _dbContext.Words)
            .AnyAsync(word => word.WordId == wordId, cancellationToken);

    public Task<bool> SenseExistsAsync(uint wordId, uint senseId, bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        (includeDeleted ? _dbContext.WordSenses.IgnoreQueryFilters() : _dbContext.WordSenses)
            .AnyAsync(sense => sense.WordId == wordId && sense.SenseId == senseId, cancellationToken);

    public Task<bool> MatchingSenseExistsAsync(uint wordId, string wordClass, string englishDefinition, CancellationToken cancellationToken = default)
    {
        var normalizedClass = wordClass.Trim().ToLowerInvariant();
        var normalizedDefinition = englishDefinition.Trim().ToLowerInvariant();
        return _dbContext.WordSenses.AnyAsync(sense => sense.WordId == wordId
            && sense.WordClass.ToLower() == normalizedClass
            && sense.EnglishDefinition.ToLower() == normalizedDefinition, cancellationToken);
    }

    public async Task<uint?> FindWordIdByKeyAsync(string wordKey, CancellationToken cancellationToken = default) =>
        await _dbContext.Words.IgnoreQueryFilters().Where(word => word.WordKey == wordKey)
            .Select(word => (uint?)word.WordId).SingleOrDefaultAsync(cancellationToken);

    public async Task<WordDetail> CreateAsync(CreateWordCommand command, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var word = new Word
        {
            Word1 = command.Word, WordKey = command.WordKey, CefrLevel = command.Cefr,
            PhoneticUk = command.PhoneticUk, PhoneticUs = command.PhoneticUs,
            ImageUrl = command.ImageUrl, IsPhrase = command.IsPhrase, Status = UserStatus.Active,
            CreatedAt = now, UpdatedAt = now,
        };
        _dbContext.Words.Add(word);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await FindDetailAsync(word.WordId, false, cancellationToken))!;
    }

    public async Task<WordDetail> CreateWithSenseAsync(
        CreateWordCommand command, CreateSenseCommand senseCommand, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sense = new VocaNova.API.Infrastructure.Persistence.Entities.WordSense
        {
            SenseOrder = senseCommand.SenseOrder, WordClass = senseCommand.WordClass,
            EnglishDefinition = senseCommand.EnglishDefinition,
            VietnameseMeaning = senseCommand.VietnameseMeaning, Status = UserStatus.Active,
        };
        var word = new Word
        {
            Word1 = command.Word, WordKey = command.WordKey, CefrLevel = command.Cefr,
            PhoneticUk = command.PhoneticUk, PhoneticUs = command.PhoneticUs,
            ImageUrl = command.ImageUrl, IsPhrase = command.IsPhrase, Status = UserStatus.Active,
            CreatedAt = now, UpdatedAt = now, WordSenses = { sense },
        };
        if (command.TopicIds is { Count: > 0 })
            foreach (var topicId in command.TopicIds.Distinct())
                word.WordTopics.Add(new PersistenceWordTopic { TopicId = topicId, IsPrimary = word.WordTopics.Count == 0 });
        _dbContext.Words.Add(word);
        await _dbContext.SaveChangesAsync(cancellationToken);
        AddExamples(word.WordId, sense, senseCommand.Examples);
        if (sense.WordExamples.Count > 0) await _dbContext.SaveChangesAsync(cancellationToken);
        return (await FindDetailAsync(word.WordId, false, cancellationToken))!;
    }

    public async Task<WordDetail?> UpdateMetadataAsync(
        uint wordId, UpdateWordCommand command, CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words.SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null) return null;
        word.Word1 = command.Word; word.WordKey = command.WordKey; word.CefrLevel = command.Cefr;
        word.PhoneticUk = command.PhoneticUk; word.PhoneticUs = command.PhoneticUs;
        word.ImageUrl = command.ImageUrl; word.IsPhrase = command.IsPhrase; word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await FindDetailAsync(wordId, false, cancellationToken);
    }

    public async Task<bool?> UpdateMissingImportMetadataAsync(
        uint wordId, ImportWordMetadata metadata, CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null) return null;
        var changed = false;
        if (string.IsNullOrWhiteSpace(word.CefrLevel) && !string.IsNullOrWhiteSpace(metadata.Cefr)) { word.CefrLevel = metadata.Cefr; changed = true; }
        if (string.IsNullOrWhiteSpace(word.PhoneticUk) && !string.IsNullOrWhiteSpace(metadata.PhoneticUk)) { word.PhoneticUk = metadata.PhoneticUk; changed = true; }
        if (string.IsNullOrWhiteSpace(word.PhoneticUs) && !string.IsNullOrWhiteSpace(metadata.PhoneticUs)) { word.PhoneticUs = metadata.PhoneticUs; changed = true; }
        if (string.IsNullOrWhiteSpace(word.ImageUrl) && !string.IsNullOrWhiteSpace(metadata.ImageUrl)) { word.ImageUrl = metadata.ImageUrl; changed = true; }
        if (metadata.IsPhrase == true && !word.IsPhrase) { word.IsPhrase = true; changed = true; }
        if (!changed) return false;
        word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetWordStatusAsync(uint wordId, string status, CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null) return false;
        word.Status = status; word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<IReadOnlyCollection<uint>> GetReferencingUserIdsAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        var listUsers = _dbContext.UserListWords.IgnoreQueryFilters()
            .Where(item => item.WordId == wordId && item.Status == UserStatus.Active).Select(item => item.UserId);
        var progressUsers = _dbContext.UserWordProgresses.Where(item => item.WordId == wordId).Select(item => item.UserId);
        return await listUsers.Concat(progressUsers).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<WordDetail?> SetImageUrlAsync(uint wordId, string? url, CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words.SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null) return null;
        word.ImageUrl = url; word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await FindDetailAsync(wordId, false, cancellationToken);
    }

    public async Task<AdminWordAudio?> UpsertAudioAsync(
        uint wordId, StoredMedia media, string? accent, CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words.SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null || accent is null) return null;
        var now = DateTime.UtcNow;
        var audio = await _dbContext.WordAudioAssets.IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId && entity.Accent == accent, cancellationToken);
        if (audio is null)
        {
            audio = new WordAudioAsset { WordId = wordId, Accent = accent, Source = AudioSource.Uploaded,
                StorageUrl = media.Url, Status = AudioStatus.Uploaded, CreatedAt = now };
            _dbContext.WordAudioAssets.Add(audio);
        }
        else { audio.Source = AudioSource.Uploaded; audio.StorageUrl = media.Url; audio.Status = AudioStatus.Uploaded; audio.CreatedAt = now; }
        word.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DictionaryAdminPersistenceMappings.ToWordAudio(audio);
    }

    public async Task<bool> SetAudioStatusAsync(uint wordId, uint audioId, string status, CancellationToken cancellationToken = default)
    {
        var audio = await _dbContext.WordAudioAssets.IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId && entity.AudioId == audioId, cancellationToken);
        if (audio is null || audio.Status == status) return false;
        audio.Status = status;
        var word = await _dbContext.Words.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is not null) word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<AdminWordSense?> CreateSenseAsync(uint wordId, CreateSenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await WordExistsAsync(wordId, cancellationToken: cancellationToken)) return null;
        var order = command.SenseOrder;
        if (order <= 0)
            order = (await _dbContext.WordSenses.Where(sense => sense.WordId == wordId)
                .Select(sense => (int?)sense.SenseOrder).MaxAsync(cancellationToken) ?? 0) + 1;
        var sense = new VocaNova.API.Infrastructure.Persistence.Entities.WordSense
        {
            WordId = wordId, SenseOrder = order, WordClass = command.WordClass,
            EnglishDefinition = command.EnglishDefinition, VietnameseMeaning = command.VietnameseMeaning,
            Status = UserStatus.Active,
        };
        AddExamples(wordId, sense, command.Examples);
        _dbContext.WordSenses.Add(sense);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DictionaryAdminPersistenceMappings.ToWordSense(sense);
    }

    public async Task<AdminWordSense?> UpdateSenseAsync(
        uint wordId, uint senseId, UpdateSenseCommand command, CancellationToken cancellationToken = default)
    {
        var sense = await _dbContext.WordSenses.Include(entity => entity.WordExamples)
            .SingleOrDefaultAsync(entity => entity.WordId == wordId && entity.SenseId == senseId, cancellationToken);
        if (sense is null) return null;
        sense.SenseOrder = command.SenseOrder; sense.WordClass = command.WordClass;
        sense.EnglishDefinition = command.EnglishDefinition; sense.VietnameseMeaning = command.VietnameseMeaning;
        if (command.Examples is not null)
        {
            var maxOrder = sense.WordExamples.Count == 0 ? -1 : sense.WordExamples.Max(example => example.OrderIndex);
            foreach (var input in command.Examples)
            {
                if (string.IsNullOrWhiteSpace(input.ExampleEn)) continue;
                var existing = input.ExampleId is { } id and > 0
                    ? sense.WordExamples.FirstOrDefault(example => example.ExampleId == id) : null;
                if (existing is not null) { existing.ExampleEn = input.ExampleEn; existing.ExampleVi = input.ExampleVi; }
                else sense.WordExamples.Add(new PersistenceWordExample { WordId = wordId, SenseId = senseId,
                    ExampleEn = input.ExampleEn, ExampleVi = input.ExampleVi, OrderIndex = ++maxOrder });
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DictionaryAdminPersistenceMappings.ToWordSense(sense);
    }

    public async Task<bool> SetSenseStatusAsync(uint wordId, uint senseId, string status, CancellationToken cancellationToken = default)
    {
        var sense = await _dbContext.WordSenses.IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId && entity.SenseId == senseId, cancellationToken);
        if (sense is null || sense.Status == status) return false;
        sense.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyDictionary<string, uint>> FindActiveTopicIdsByNamesAsync(
        IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        if (names.Count == 0) return new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var normalized = names.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim().ToLowerInvariant()).Distinct().ToArray();
        var topics = await _dbContext.Topics.AsNoTracking()
            .Where(topic => normalized.Contains(topic.TopicName.ToLower())
                || (topic.TopicNameVi != null && normalized.Contains(topic.TopicNameVi.ToLower())))
            .Select(topic => new { topic.TopicId, topic.TopicName, topic.TopicNameVi }).ToListAsync(cancellationToken);
        var result = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        foreach (var topic in topics) { result[topic.TopicName.Trim()] = topic.TopicId; if (!string.IsNullOrWhiteSpace(topic.TopicNameVi)) result[topic.TopicNameVi.Trim()] = topic.TopicId; }
        return result;
    }

    public async Task<int> AddTopicsAsync(uint wordId, IReadOnlyCollection<uint> topicIds, CancellationToken cancellationToken = default)
    {
        if (topicIds.Count == 0) return 0;
        var word = await _dbContext.Words.IgnoreQueryFilters().Include(entity => entity.WordTopics)
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null) return 0;
        var existing = word.WordTopics.Select(link => link.TopicId).ToHashSet(); var added = 0;
        foreach (var topicId in topicIds.Distinct())
            if (existing.Add(topicId)) { word.WordTopics.Add(new PersistenceWordTopic { WordId = wordId, TopicId = topicId, IsPrimary = word.WordTopics.Count == 0 }); added++; }
        if (added == 0) return 0;
        word.UpdatedAt = DateTime.UtcNow; await _dbContext.SaveChangesAsync(cancellationToken); return added;
    }

    private async Task<WordDetail?> FindDetailAsync(uint wordId, bool includeDeleted, CancellationToken cancellationToken)
    {
        var query = includeDeleted ? _dbContext.Words.IgnoreQueryFilters() : _dbContext.Words;
        var word = await query.AsNoTracking().AsSplitQuery()
            .Include(entity => entity.WordSenses).ThenInclude(sense => sense.WordExamples)
            .Include(entity => entity.WordSenses).ThenInclude(sense => sense.WordRelations).ThenInclude(relation => relation.RelatedWordNavigation)
            .Include(entity => entity.WordExamples)
            .Include(entity => entity.WordRelationwords).ThenInclude(relation => relation.RelatedWordNavigation)
            .Include(entity => entity.WordAudioAssets)
            .Include(entity => entity.WordDerivedFormwords).ThenInclude(form => form.DerivedWordNavigation)
            .Include(entity => entity.WordIdioms)
            .Include(entity => entity.WordTopics).ThenInclude(link => link.Topic)
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        return word is null ? null : DictionaryAdminPersistenceMappings.ToWordDetail(word);
    }

    private static void AddExamples(uint wordId, VocaNova.API.Infrastructure.Persistence.Entities.WordSense sense,
        IReadOnlyList<SenseExampleInput>? examples)
    {
        if (examples is null) return; var order = 0;
        foreach (var input in examples)
            if (!string.IsNullOrWhiteSpace(input.ExampleEn)) sense.WordExamples.Add(new PersistenceWordExample
            { WordId = wordId, SenseId = sense.SenseId == 0 ? null : sense.SenseId,
                ExampleEn = input.ExampleEn, ExampleVi = input.ExampleVi, OrderIndex = order++ });
    }
}
