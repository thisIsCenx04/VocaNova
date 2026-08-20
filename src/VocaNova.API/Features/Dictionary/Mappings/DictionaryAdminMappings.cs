using VocaNova.API.Common.Models;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.Contracts.Requests;
using VocaNova.API.Features.Dictionary.Contracts.Responses;

namespace VocaNova.API.Features.Dictionary.Mappings;

public static class DictionaryAdminMappings
{
    public static AdminWordQuery ToBusinessQuery(this AdminWordQueryRequest request) =>
        new(request.Q, request.Cefr, request.TopicId, request.WordType, request.Status,
            request.IncludeDeleted, request.Page, request.Limit, request.SortBy, request.SortDirection);

    public static AdminTopicQuery ToBusinessQuery(this AdminTopicQueryRequest request) =>
        new(request.Q, request.Status, request.IncludeDeleted);

    public static CreateWordCommand ToBusinessCommand(this CreateWordRequest request) =>
        new(request.Word ?? string.Empty, string.Empty, request.Cefr, request.PhoneticUk,
            request.PhoneticUs, request.ImageUrl, request.IsPhrase);

    public static UpdateWordCommand ToBusinessCommand(this UpdateWordRequest request) =>
        new(request.Word ?? string.Empty, string.Empty, request.Cefr, request.PhoneticUk,
            request.PhoneticUs, request.ImageUrl, request.IsPhrase);

    public static CreateSenseCommand ToBusinessCommand(this CreateSenseRequest request) =>
        new(request.SenseOrder, request.WordClass ?? string.Empty,
            request.EnglishDefinition ?? string.Empty, request.VietnameseMeaning,
            request.Examples?.Select(ToBusinessInput).ToArray());

    public static UpdateSenseCommand ToBusinessCommand(this UpdateSenseRequest request) =>
        new(request.SenseOrder, request.WordClass ?? string.Empty,
            request.EnglishDefinition ?? string.Empty, request.VietnameseMeaning,
            request.Examples?.Select(ToBusinessInput).ToArray());

    public static CreateTopicCommand ToBusinessCommand(this CreateTopicRequest request) =>
        new(request.TopicName ?? string.Empty, request.TopicNameVi, request.Icon, request.WordIds);

    public static UpdateTopicCommand ToBusinessCommand(this UpdateTopicRequest request) =>
        new(request.TopicName ?? string.Empty, request.TopicNameVi, request.Icon, request.WordIds);

    public static UploadedContent? ToUploadedContent(this IFormFile? file, Stream? content) =>
        file is null || content is null
            ? null
            : new UploadedContent(file.FileName, file.ContentType, file.Length, content);

    public static PagedResult<AdminWordListItemResponse> ToResponse(
        this PagedCollection<AdminWordListItem> words) =>
        new(words.Items.Select(ToResponse).ToArray(), words.Page, words.Limit, words.TotalItems);

    public static IReadOnlyCollection<AdminTopicResponse> ToResponse(
        this IReadOnlyCollection<AdminTopic> topics) =>
        topics.Select(topic => new AdminTopicResponse(topic.TopicId, topic.TopicName,
            topic.TopicNameVi, topic.Icon, topic.Status, topic.WordCount)).ToArray();

    public static BulkImportResponse ToResponse(this BulkImportResult result) =>
        new(result.ImportedWords, result.ImportedSenses, result.Skipped,
            result.Errors.Select(error => new BulkImportErrorResponse(error.Row, error.Column, error.Message)).ToArray(),
            result.UpdatedWords, result.ImportedTopics, result.ImportedExamples);

    public static WordSenseResponse ToResponse(this WordSense sense) =>
        new(sense.SenseId, sense.Order, sense.WordClass, sense.EnglishDefinition,
            sense.VietnameseMeaning,
            sense.Examples.Select(example => new WordExampleResponse(example.ExampleId,
                example.SenseId, example.ExampleEn, example.ExampleVi, example.Order)).ToArray(),
            sense.Relations.Select(relation => new WordRelationResponse(relation.RelationId,
                relation.SenseId, relation.RelationType, relation.RelatedWord,
                relation.LinkedWordId, relation.IsQuizEligible)).ToArray());

    public static WordAudioResponse ToResponse(this WordAudio audio) =>
        new(audio.AudioId, audio.Accent, audio.Source, audio.Url, audio.Status);

    public static TopicSummaryResponse ToResponse(this TopicSummary topic) =>
        new(topic.TopicId, topic.Name, topic.NameVi, topic.Icon, topic.WordCount);

    private static AdminWordListItemResponse ToResponse(AdminWordListItem word) =>
        new(word.WordId, word.Word, word.Cefr, word.Phonetic, word.Status, word.ImageUrl,
            word.PrimaryMeaning,
            word.Topics.Select(topic => new WordTopicResponse(topic.TopicId, topic.Name,
                topic.NameVi, topic.Icon)).ToArray(),
            word.WordType);

    private static SenseExampleInput ToBusinessInput(SenseExampleRequest input) =>
        new(input.ExampleId, input.ExampleEn ?? string.Empty, input.ExampleVi);
}
