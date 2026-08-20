using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Contracts.Responses;

namespace VocaNova.API.Features.Dictionary.Mappings;

public static class DictionaryMappings
{
    public static WordSearchQuery ToBusinessQuery(this WordSearchRequest request) =>
        new(request.Q, request.Page, request.Limit, request.Cefr, request.TopicId, request.IsPhrase);

    public static TopicWordsQuery ToBusinessQuery(this TopicWordsRequest request) =>
        new(request.Page, request.Limit);

    public static PagedResult<WordSummaryResponse> ToResponse(
        this PagedCollection<WordSummary> words) =>
        new(
            words.Items.Select(ToResponse).ToArray(),
            words.Page,
            words.Limit,
            words.TotalItems);

    public static IReadOnlyCollection<TopicSummaryResponse> ToResponse(
        this IReadOnlyCollection<TopicSummary> topics) =>
        topics.Select(topic => new TopicSummaryResponse(
            topic.TopicId,
            topic.Name,
            topic.NameVi,
            topic.Icon,
            topic.WordCount)).ToArray();

    public static WordDetailResponse ToResponse(this WordDetail word) =>
        new(
            word.WordId,
            word.Word,
            word.WordKey,
            word.Cefr,
            word.PhoneticUk,
            word.PhoneticUs,
            word.ImageUrl,
            word.IsPhrase,
            word.Senses.Select(sense => new WordSenseResponse(
                sense.SenseId,
                sense.Order,
                sense.WordClass,
                sense.EnglishDefinition,
                sense.VietnameseMeaning,
                sense.Examples.Select(ToResponse).ToArray(),
                sense.Relations.Select(ToResponse).ToArray())).ToArray(),
            word.Examples.Select(ToResponse).ToArray(),
            word.Relations.Select(ToResponse).ToArray(),
            word.Audio.Select(audio => new WordAudioResponse(
                audio.AudioId,
                audio.Accent,
                audio.Source,
                audio.Url,
                audio.Status)).ToArray(),
            word.DerivedForms.Select(form => new WordDerivedFormResponse(
                form.DerivedId,
                form.DerivedWord,
                form.LinkedWordId,
                form.WordClass)).ToArray(),
            word.Idioms.Select(idiom => new WordIdiomResponse(
                idiom.IdiomId,
                idiom.IdiomText,
                idiom.MeaningEn,
                idiom.MeaningVi)).ToArray(),
            word.Topics.Select(topic => new WordTopicResponse(
                topic.TopicId,
                topic.Name,
                topic.NameVi,
                topic.Icon)).ToArray(),
            word.Status,
            word.CreatedAt,
            word.UpdatedAt);

    private static WordSummaryResponse ToResponse(WordSummary word) =>
        new(word.WordId, word.Word, word.Phonetic, word.Cefr, word.PrimaryMeaning, word.ImageUrl);

    private static WordExampleResponse ToResponse(WordExample example) =>
        new(example.ExampleId, example.SenseId, example.ExampleEn, example.ExampleVi, example.Order);

    private static WordRelationResponse ToResponse(WordRelation relation) =>
        new(
            relation.RelationId,
            relation.SenseId,
            relation.RelationType,
            relation.RelatedWord,
            relation.LinkedWordId,
            relation.IsQuizEligible);
}
