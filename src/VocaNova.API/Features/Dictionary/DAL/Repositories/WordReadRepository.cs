using Microsoft.EntityFrameworkCore;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.DAL.Mappings;
using VocaNova.API.Infrastructure.Persistence;

namespace VocaNova.API.Features.Dictionary.DAL.Repositories;

public sealed class WordReadRepository : IWordReadRepository
{
    private readonly VocaNovaDbContext _dbContext;

    public WordReadRepository(VocaNovaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedCollection<WordSummary>> SearchAsync(
        string? normalizedQuery,
        int page,
        int limit,
        string? cefr,
        uint? topicId,
        bool? isPhrase,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words.AsNoTracking().AsQueryable();
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
            query = query.Where(word => word.WordTopics.Any(
                wordTopic => wordTopic.TopicId == topicId.Value));
        }
        if (isPhrase.HasValue)
        {
            query = query.Where(word => word.IsPhrase == isPhrase.Value);
        }

        var ordered = query.OrderBy(word => word.WordKey).ThenBy(word => word.WordId);
        var totalItems = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(DictionaryPersistenceMappings.ToWordSummary)
            .ToListAsync(cancellationToken);

        return new PagedCollection<WordSummary>(items, page, limit, totalItems);
    }

    public async Task<WordDetail?> FindDetailAsync(
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

        return word is null ? null : DictionaryPersistenceMappings.ToWordDetail(word);
    }

    public async Task<IReadOnlyCollection<uint>> GetDailyCandidateWordIdsAsync(
        bool requirePlayableAudio,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Words
            .AsNoTracking()
            .Where(word => word.WordSenses.Any());
        if (requirePlayableAudio)
        {
            var playableStatuses = AudioStatus.Playable.ToArray();
            query = query.Where(word => word.WordAudioAssets.Any(audio =>
                playableStatuses.Contains(audio.Status)
                && !string.IsNullOrWhiteSpace(audio.StorageUrl)));
        }

        return await query
            .OrderBy(word => word.WordId)
            .Select(word => word.WordId)
            .ToListAsync(cancellationToken);
    }
}
