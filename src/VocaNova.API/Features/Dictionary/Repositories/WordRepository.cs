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
                .Select(audio => new WordAudioDto(
                    audio.AudioId,
                    audio.Accent,
                    audio.Source,
                    audio.StorageUrl!,
                    audio.Status))
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
                .OrderByDescending(wordTopic => wordTopic.IsPrimary)
                .ThenBy(wordTopic => wordTopic.Topic.TopicName)
                .Select(wordTopic => new WordTopicDto(
                    wordTopic.TopicId,
                    wordTopic.Topic.TopicName,
                    wordTopic.Topic.TopicNameVi,
                    wordTopic.Topic.Icon,
                    wordTopic.IsPrimary))
                .ToArray());
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
}
