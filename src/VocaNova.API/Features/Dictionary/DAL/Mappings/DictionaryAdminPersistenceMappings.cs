using System.Linq.Expressions;
using VocaNova.API.Common.Constants;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Infrastructure.Persistence.Entities;
using AdminWordSense = VocaNova.API.Features.Dictionary.BLL.Models.WordSense;

namespace VocaNova.API.Features.Dictionary.DAL.Mappings;

internal static class DictionaryAdminPersistenceMappings
{
    public static readonly Expression<Func<Word, AdminWordListItem>> ToAdminWordListItem =
        word => new AdminWordListItem(
            word.WordId, word.Word1, word.CefrLevel, word.PhoneticUs ?? word.PhoneticUk,
            word.Status, word.ImageUrl,
            word.WordSenses.OrderBy(sense => sense.SenseOrder)
                .Select(sense => sense.VietnameseMeaning).FirstOrDefault(),
            word.WordTopics.OrderBy(link => link.Topic.TopicName)
                .Select(link => new VocaNova.API.Features.Dictionary.BLL.Models.WordTopic(
                    link.TopicId, link.Topic.TopicName, link.Topic.TopicNameVi, link.Topic.Icon)).ToArray(),
            word.WordSenses.OrderBy(sense => sense.SenseOrder)
                .Select(sense => sense.WordClass).FirstOrDefault());

    public static WordDetail ToWordDetail(Word word) => DictionaryPersistenceMappings.ToWordDetail(word);

    public static AdminWordSense ToWordSense(VocaNova.API.Infrastructure.Persistence.Entities.WordSense sense) =>
        new(sense.SenseId, sense.SenseOrder, sense.WordClass, sense.EnglishDefinition,
            sense.VietnameseMeaning,
            sense.WordExamples.OrderBy(example => example.OrderIndex).ThenBy(example => example.ExampleId)
                .Select(example => new VocaNova.API.Features.Dictionary.BLL.Models.WordExample(
                    example.ExampleId, example.SenseId, example.ExampleEn, example.ExampleVi, example.OrderIndex)).ToArray(),
            sense.WordRelations.OrderBy(relation => relation.RelationType).ThenBy(relation => relation.RelationId)
                .Select(relation => new VocaNova.API.Features.Dictionary.BLL.Models.WordRelation(
                    relation.RelationId, relation.SenseId, relation.RelationType, relation.RelatedWord,
                    relation.RelatedWordNavigation?.WordId, relation.IsQuizEligible ?? true)).ToArray());

    public static WordAudio ToWordAudio(WordAudioAsset audio) =>
        new(audio.AudioId, audio.Accent, audio.Source, audio.StorageUrl!, audio.Status);

    public static TopicSummary ToTopicSummary(Topic topic, int wordCount) =>
        new(topic.TopicId, topic.TopicName, topic.TopicNameVi, topic.Icon, wordCount);
}
