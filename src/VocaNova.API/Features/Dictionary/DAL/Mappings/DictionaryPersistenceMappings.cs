using System.Linq.Expressions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Common.Constants;
using VocaNova.API.Infrastructure.Persistence.Entities;

namespace VocaNova.API.Features.Dictionary.DAL.Mappings;

internal static class DictionaryPersistenceMappings
{
    public static readonly Expression<Func<Word, WordSummary>> ToWordSummary =
        word => new WordSummary(
            word.WordId,
            word.Word1,
            word.PhoneticUs ?? word.PhoneticUk,
            word.CefrLevel,
            word.WordSenses
                .OrderBy(sense => sense.SenseOrder)
                .Select(sense => sense.VietnameseMeaning)
                .FirstOrDefault(),
            word.ImageUrl);

    public static readonly Expression<Func<Topic, TopicSummary>> ToTopicSummary =
        topic => new TopicSummary(
            topic.TopicId,
            topic.TopicName,
            topic.TopicNameVi,
            topic.Icon,
            topic.WordTopics.Count);

    public static WordDetail ToWordDetail(Word word)
    {
        var senses = word.WordSenses
            .OrderBy(sense => sense.SenseOrder)
            .ThenBy(sense => sense.SenseId)
            .Select(sense => new VocaNova.API.Features.Dictionary.BLL.Models.WordSense(
                sense.SenseId,
                sense.SenseOrder,
                sense.WordClass,
                sense.EnglishDefinition,
                sense.VietnameseMeaning,
                sense.WordExamples
                    .OrderBy(example => example.OrderIndex)
                    .ThenBy(example => example.ExampleId)
                    .Select(ToWordExample)
                    .ToArray(),
                sense.WordRelations
                    .OrderBy(relation => relation.RelationType)
                    .ThenBy(relation => relation.RelationId)
                    .Select(ToWordRelation)
                    .ToArray()))
            .ToArray();

        return new WordDetail(
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
                .Select(ToWordExample)
                .ToArray(),
            word.WordRelationwords
                .Where(relation => relation.SenseId is null)
                .OrderBy(relation => relation.RelationType)
                .ThenBy(relation => relation.RelationId)
                .Select(ToWordRelation)
                .ToArray(),
            word.WordAudioAssets
                .Where(audio => AudioStatus.Playable.Contains(audio.Status)
                    && !string.IsNullOrWhiteSpace(audio.StorageUrl))
                .OrderBy(audio => audio.Accent)
                .ThenBy(audio => audio.AudioId)
                .Select(audio => new WordAudio(
                    audio.AudioId,
                    audio.Accent,
                    audio.Source,
                    audio.StorageUrl!,
                    audio.Status))
                .ToArray(),
            word.WordDerivedFormwords
                .OrderBy(derivedForm => derivedForm.DerivedWord)
                .ThenBy(derivedForm => derivedForm.DerivedId)
                .Select(derivedForm => new VocaNova.API.Features.Dictionary.BLL.Models.WordDerivedForm(
                    derivedForm.DerivedId,
                    derivedForm.DerivedWord,
                    derivedForm.DerivedWordNavigation?.WordId,
                    derivedForm.WordClass))
                .ToArray(),
            word.WordIdioms
                .OrderBy(idiom => idiom.IdiomText)
                .ThenBy(idiom => idiom.IdiomId)
                .Select(idiom => new VocaNova.API.Features.Dictionary.BLL.Models.WordIdiom(
                    idiom.IdiomId,
                    idiom.IdiomText,
                    idiom.MeaningEn,
                    idiom.MeaningVi))
                .ToArray(),
            word.WordTopics
                .OrderBy(wordTopic => wordTopic.Topic.TopicName)
                .Select(wordTopic => new VocaNova.API.Features.Dictionary.BLL.Models.WordTopic(
                    wordTopic.TopicId,
                    wordTopic.Topic.TopicName,
                    wordTopic.Topic.TopicNameVi,
                    wordTopic.Topic.Icon))
                .ToArray(),
            word.Status,
            word.CreatedAt,
            word.UpdatedAt);
    }

    private static VocaNova.API.Features.Dictionary.BLL.Models.WordExample ToWordExample(
        VocaNova.API.Infrastructure.Persistence.Entities.WordExample example) =>
        new(
            example.ExampleId,
            example.SenseId,
            example.ExampleEn,
            example.ExampleVi,
            example.OrderIndex);

    private static VocaNova.API.Features.Dictionary.BLL.Models.WordRelation ToWordRelation(
        VocaNova.API.Infrastructure.Persistence.Entities.WordRelation relation) =>
        new(
            relation.RelationId,
            relation.SenseId,
            relation.RelationType,
            relation.RelatedWord,
            relation.RelatedWordNavigation?.WordId,
            relation.IsQuizEligible ?? true);
}
