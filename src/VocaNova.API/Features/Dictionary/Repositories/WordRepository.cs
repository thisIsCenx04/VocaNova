using Microsoft.EntityFrameworkCore;
using VocaNova.API.Common.Constants;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;
using VocaNova.API.Infrastructure.Persistence;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Dictionary.Repositories;

public sealed class WordRepository : IWordRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public WordRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<WordSummaryDto>> SearchAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(word => EF.Functions.Like(word.WordKey, normalizedQuery + "%"));
        }

        if (!string.IsNullOrWhiteSpace(cefr))
        {
            query = query.Where(word => word.CefrLevel == cefr);
        }

        if (topicId.HasValue)
        {
            query = query.Where(word => word.WordTopics.Any(wordTopic => wordTopic.TopicId == topicId.Value));
        }

        if (isPhrase.HasValue)
        {
            query = query.Where(word => word.IsPhrase == isPhrase.Value);
        }

        return query
            .OrderBy(word => word.WordKey)
            .ThenBy(word => word.WordId)
            .Select(word => new WordSummaryDto(
                word.WordId,
                word.Word1,
                word.PhoneticUs ?? word.PhoneticUk,
                word.CefrLevel,
                word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                word.ImageUrl))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public Task<PagedResult<AdminWordListItemDto>> SearchAdminAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        string? status,
        bool includeDeleted,
        string? wordType,
        CancellationToken cancellationToken = default)
    {
        // includeDeleted = bỏ global query filter để thấy cả từ status='deleted'.
        var query = includeDeleted
            ? _dbContext.Words.IgnoreQueryFilters().AsNoTracking()
            : _dbContext.Words.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(word => EF.Functions.Like(word.WordKey, normalizedQuery + "%"));
        }

        if (!string.IsNullOrWhiteSpace(cefr))
        {
            query = query.Where(word => word.CefrLevel == cefr);
        }

        if (topicId.HasValue)
        {
            query = query.Where(word => word.WordTopics.Any(wordTopic => wordTopic.TopicId == topicId.Value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(word => word.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(wordType))
        {
            query = query.Where(word => word.WordSenses.Any(sense => sense.WordClass == wordType));
        }

        // Quản trị: từ mới tạo lên đầu (thay vì sắp alphabet — đầu danh sách toàn ký hiệu/từ rỗng do import từ điển).
        return query
            .OrderByDescending(word => word.CreatedAt)
            .ThenByDescending(word => word.WordId)
            .Select(word => new AdminWordListItemDto(
                word.WordId,
                word.Word1,
                word.CefrLevel,
                word.PhoneticUs ?? word.PhoneticUk,
                word.Status,
                word.ImageUrl,
                word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .Select(sense => sense.VietnameseMeaning)
                    .FirstOrDefault(),
                word.WordTopics
                    .OrderBy(wordTopic => wordTopic.Topic.TopicName)
                    .Select(wordTopic => new WordTopicDto(
                        wordTopic.TopicId,
                        wordTopic.Topic.TopicName,
                        wordTopic.Topic.TopicNameVi,
                        wordTopic.Topic.Icon))
                    .ToList(),
                word.WordSenses
                    .OrderBy(sense => sense.SenseOrder)
                    .Select(sense => sense.WordClass)
                    .FirstOrDefault()))
            .ToPagedResultAsync(page, limit, cancellationToken);
    }

    public async Task<WordDetailDto?> FindDetailAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .AsNoTracking()
            .AsSplitQuery()
            .Include(entity => entity.WordSenses)
                .ThenInclude(sense => sense.WordExamples)
            .Include(entity => entity.WordSenses)
                .ThenInclude(sense => sense.WordRelations)
                    .ThenInclude(relation => relation.RelatedWordNavigation)
            .Include(entity => entity.WordExamples)
            .Include(entity => entity.WordRelationwords)
                .ThenInclude(relation => relation.RelatedWordNavigation)
            .Include(entity => entity.WordAudioAssets)
            .Include(entity => entity.WordDerivedFormwords)
                .ThenInclude(derivedForm => derivedForm.DerivedWordNavigation)
            .Include(entity => entity.WordIdioms)
            .Include(entity => entity.WordTopics)
                .ThenInclude(wordTopic => wordTopic.Topic)
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);

        return word is null ? null : MapDetail(word);
    }

    public Task<bool> WordKeyExistsAsync(
        string wordKey,
        uint? excludingWordId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Words
            .IgnoreQueryFilters()
            .AnyAsync(
                word => word.WordKey == wordKey
                    && (!excludingWordId.HasValue || word.WordId != excludingWordId.Value),
                cancellationToken);
    }

    public async Task<uint?> FindWordIdByKeyAsync(
        string wordKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Words
            .IgnoreQueryFilters()
            .Where(word => word.WordKey == wordKey)
            .Select(word => (uint?)word.WordId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WordDetailDto> CreateAsync(
        CreateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var word = new Word
        {
            Word1 = request.Word!.Trim(),
            WordKey = wordKey,
            CefrLevel = NormalizeNullable(request.Cefr),
            PhoneticUk = NormalizeNullable(request.PhoneticUk),
            PhoneticUs = NormalizeNullable(request.PhoneticUs),
            ImageUrl = NormalizeNullable(request.ImageUrl),
            IsPhrase = request.IsPhrase,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.Words.Add(word);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await FindDetailAsync(word.WordId, cancellationToken))!;
    }

    public async Task<WordDetailDto> CreateWithSenseAsync(
        CreateWordRequest wordRequest,
        string wordKey,
        CreateSenseRequest senseRequest,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var word = new Word
        {
            Word1 = wordRequest.Word!.Trim(),
            WordKey = wordKey,
            CefrLevel = NormalizeNullable(wordRequest.Cefr),
            PhoneticUk = NormalizeNullable(wordRequest.PhoneticUk),
            PhoneticUs = NormalizeNullable(wordRequest.PhoneticUs),
            ImageUrl = NormalizeNullable(wordRequest.ImageUrl),
            IsPhrase = wordRequest.IsPhrase,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            WordSenses =
            {
                new WordSense
                {
                    SenseOrder = senseRequest.SenseOrder,
                    WordClass = senseRequest.WordClass!.Trim(),
                    EnglishDefinition = senseRequest.EnglishDefinition!.Trim(),
                    VietnameseMeaning = NormalizeNullable(senseRequest.VietnameseMeaning),
                },
            },
        };

        _dbContext.Words.Add(word);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await FindDetailAsync(word.WordId, cancellationToken))!;
    }

    public async Task<WordDetailDto?> UpdateMetadataAsync(
        uint wordId,
        UpdateWordRequest request,
        string wordKey,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null)
        {
            return null;
        }

        word.Word1 = request.Word!.Trim();
        word.WordKey = wordKey;
        word.CefrLevel = NormalizeNullable(request.Cefr);
        word.PhoneticUk = NormalizeNullable(request.PhoneticUk);
        word.PhoneticUs = NormalizeNullable(request.PhoneticUs);
        word.ImageUrl = NormalizeNullable(request.ImageUrl);
        word.IsPhrase = request.IsPhrase;
        word.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await FindDetailAsync(word.WordId, cancellationToken);
    }

    public async Task<bool> SetStatusAsync(
        uint wordId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null)
        {
            return false;
        }

        word.Status = status;
        word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<WordDetailDto?> SetImageUrlAsync(
        uint wordId,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null)
        {
            return null;
        }

        word.ImageUrl = NormalizeNullable(imageUrl);
        word.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await FindDetailAsync(word.WordId, cancellationToken);
    }

    public Task<bool> WordExistsAsync(
        uint wordId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Words.AnyAsync(word => word.WordId == wordId, cancellationToken);
    }

    public async Task<WordAudioDto?> UpsertAudioAsync(
        uint wordId,
        string accent,
        string storageUrl,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var audio = await _dbContext.WordAudioAssets
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                entity => entity.WordId == wordId && entity.Accent == accent,
                cancellationToken);
        if (audio is null)
        {
            audio = new WordAudioAsset
            {
                WordId = wordId,
                Accent = accent,
                Source = AudioSource.Uploaded,
                StorageUrl = storageUrl,
                Status = AudioStatus.Uploaded,
                CreatedAt = now,
            };
            _dbContext.WordAudioAssets.Add(audio);
        }
        else
        {
            audio.Source = AudioSource.Uploaded;
            audio.StorageUrl = storageUrl;
            audio.Status = AudioStatus.Uploaded;
            audio.CreatedAt = now;
        }

        word.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAudio(audio);
    }

    public async Task<bool> SetAudioStatusAsync(
        uint wordId,
        uint audioId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var audio = await _dbContext.WordAudioAssets
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                entity => entity.WordId == wordId && entity.AudioId == audioId,
                cancellationToken);
        if (audio is null || audio.Status == status)
        {
            return false;
        }

        audio.Status = status;
        var word = await _dbContext.Words
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is not null)
        {
            word.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WordSenseDto?> CreateSenseAsync(
        uint wordId,
        CreateSenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await WordExistsAsync(wordId, cancellationToken))
        {
            return null;
        }

        var sense = new WordSense
        {
            WordId = wordId,
            SenseOrder = request.SenseOrder,
            WordClass = request.WordClass!.Trim(),
            EnglishDefinition = request.EnglishDefinition!.Trim(),
            VietnameseMeaning = NormalizeNullable(request.VietnameseMeaning),
        };

        // Ví dụ gửi kèm (nếu có) — gắn vào navigation để EF chèn cùng sense.
        if (request.Examples is { Count: > 0 })
        {
            var order = 0;
            foreach (var example in request.Examples)
            {
                if (string.IsNullOrWhiteSpace(example.ExampleEn))
                {
                    continue;
                }

                sense.WordExamples.Add(new WordExample
                {
                    WordId = wordId,
                    ExampleEn = example.ExampleEn.Trim(),
                    ExampleVi = NormalizeNullable(example.ExampleVi),
                    OrderIndex = order++,
                });
            }
        }

        _dbContext.WordSenses.Add(sense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSense(sense);
    }

    public async Task<WordSenseDto?> CreateNextSenseAsync(
        uint wordId,
        string wordClass,
        string englishDefinition,
        string? vietnameseMeaning,
        CancellationToken cancellationToken = default)
    {
        var word = await _dbContext.Words
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(entity => entity.WordId == wordId, cancellationToken);
        if (word is null)
        {
            return null;
        }

        var nextOrder = await _dbContext.WordSenses
            .Where(sense => sense.WordId == wordId)
            .Select(sense => (int?)sense.SenseOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var sense = new WordSense
        {
            WordId = wordId,
            SenseOrder = nextOrder + 1,
            WordClass = wordClass.Trim(),
            EnglishDefinition = englishDefinition.Trim(),
            VietnameseMeaning = NormalizeNullable(vietnameseMeaning),
        };

        word.UpdatedAt = DateTime.UtcNow;
        _dbContext.WordSenses.Add(sense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSense(sense);
    }

    public async Task<WordSenseDto?> UpdateSenseAsync(
        uint wordId,
        uint senseId,
        UpdateSenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var sense = await _dbContext.WordSenses
            .Include(entity => entity.WordExamples)
            .SingleOrDefaultAsync(
                entity => entity.WordId == wordId && entity.SenseId == senseId,
                cancellationToken);
        if (sense is null)
        {
            return null;
        }

        sense.SenseOrder = request.SenseOrder;
        sense.WordClass = request.WordClass!.Trim();
        sense.EnglishDefinition = request.EnglishDefinition!.Trim();
        sense.VietnameseMeaning = NormalizeNullable(request.VietnameseMeaning);

        // Upsert ví dụ: có example_id → cập nhật; không có → thêm mới. KHÔNG xóa ví dụ vắng mặt
        // (xóa mềm ví dụ chưa hỗ trợ — bảng word_examples chưa có cột trạng thái).
        if (request.Examples is not null)
        {
            var maxOrder = sense.WordExamples.Count == 0
                ? -1
                : sense.WordExamples.Max(example => example.OrderIndex);

            foreach (var input in request.Examples)
            {
                if (string.IsNullOrWhiteSpace(input.ExampleEn))
                {
                    continue;
                }

                WordExample? existing = input.ExampleId is { } id && id > 0
                    ? sense.WordExamples.FirstOrDefault(example => example.ExampleId == id)
                    : null;

                if (existing is not null)
                {
                    existing.ExampleEn = input.ExampleEn.Trim();
                    existing.ExampleVi = NormalizeNullable(input.ExampleVi);
                }
                else
                {
                    sense.WordExamples.Add(new WordExample
                    {
                        WordId = wordId,
                        SenseId = senseId,
                        ExampleEn = input.ExampleEn.Trim(),
                        ExampleVi = NormalizeNullable(input.ExampleVi),
                        OrderIndex = ++maxOrder,
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSense(sense);
    }

    private static WordDetailDto MapDetail(Word word)
    {
        var senses = word.WordSenses
            .OrderBy(sense => sense.SenseOrder)
            .ThenBy(sense => sense.SenseId)
            .Select(sense => new WordSenseDto(
                sense.SenseId,
                sense.SenseOrder,
                sense.WordClass,
                sense.EnglishDefinition,
                sense.VietnameseMeaning,
                sense.WordExamples
                    .OrderBy(example => example.OrderIndex)
                    .ThenBy(example => example.ExampleId)
                    .Select(MapExample)
                    .ToArray(),
                sense.WordRelations
                    .OrderBy(relation => relation.RelationType)
                    .ThenBy(relation => relation.RelationId)
                    .Select(MapRelation)
                    .ToArray()))
            .ToArray();

        return new WordDetailDto(
            word.WordId,
            word.Word1,
            word.WordKey,
            word.CefrLevel,
            word.PhoneticUk,
            word.PhoneticUs,
            word.ImageUrl,
            word.IsPhrase,
            senses,
            word.WordExamples
                .Where(example => example.SenseId is null)
                .OrderBy(example => example.OrderIndex)
                .ThenBy(example => example.ExampleId)
                .Select(MapExample)
                .ToArray(),
            word.WordRelationwords
                .Where(relation => relation.SenseId is null)
                .OrderBy(relation => relation.RelationType)
                .ThenBy(relation => relation.RelationId)
                .Select(MapRelation)
                .ToArray(),
            word.WordAudioAssets
                .Where(audio => AudioStatus.Playable.Contains(audio.Status)
                    && !string.IsNullOrWhiteSpace(audio.StorageUrl))
                .OrderBy(audio => audio.Accent)
                .ThenBy(audio => audio.AudioId)
                .Select(MapAudio)
                .ToArray(),
            word.WordDerivedFormwords
                .OrderBy(derivedForm => derivedForm.DerivedWord)
                .ThenBy(derivedForm => derivedForm.DerivedId)
                .Select(derivedForm => new WordDerivedFormDto(
                    derivedForm.DerivedId,
                    derivedForm.DerivedWord,
                    derivedForm.DerivedWordNavigation?.WordId,
                    derivedForm.WordClass))
                .ToArray(),
            word.WordIdioms
                .OrderBy(idiom => idiom.IdiomText)
                .ThenBy(idiom => idiom.IdiomId)
                .Select(idiom => new WordIdiomDto(
                    idiom.IdiomId,
                    idiom.IdiomText,
                    idiom.MeaningEn,
                    idiom.MeaningVi))
                .ToArray(),
            word.WordTopics
                .OrderBy(wordTopic => wordTopic.Topic.TopicName)
                .Select(wordTopic => new WordTopicDto(
                    wordTopic.TopicId,
                    wordTopic.Topic.TopicName,
                    wordTopic.Topic.TopicNameVi,
                    wordTopic.Topic.Icon))
                .ToArray(),
            word.Status,
            word.CreatedAt,
            word.UpdatedAt);
    }

    private static WordExampleDto MapExample(WordExample example)
    {
        return new WordExampleDto(
            example.ExampleId,
            example.SenseId,
            example.ExampleEn,
            example.ExampleVi,
            example.OrderIndex);
    }

    private static WordSenseDto MapSense(WordSense sense)
    {
        return new WordSenseDto(
            sense.SenseId,
            sense.SenseOrder,
            sense.WordClass,
            sense.EnglishDefinition,
            sense.VietnameseMeaning,
            sense.WordExamples
                .OrderBy(example => example.OrderIndex)
                .ThenBy(example => example.ExampleId)
                .Select(MapExample)
                .ToArray(),
            Array.Empty<WordRelationDto>());
    }

    private static WordAudioDto MapAudio(WordAudioAsset audio)
    {
        return new WordAudioDto(
            audio.AudioId,
            audio.Accent,
            audio.Source,
            audio.StorageUrl!,
            audio.Status);
    }

    private static WordRelationDto MapRelation(WordRelation relation)
    {
        return new WordRelationDto(
            relation.RelationId,
            relation.SenseId,
            relation.RelationType,
            relation.RelatedWord,
            relation.RelatedWordNavigation?.WordId,
            relation.IsQuizEligible ?? true);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
