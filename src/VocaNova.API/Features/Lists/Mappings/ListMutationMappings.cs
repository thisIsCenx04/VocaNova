using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Features.Lists.Contracts.Requests;
using VocaNova.API.Features.Lists.Contracts.Responses;

namespace VocaNova.API.Features.Lists.Mappings;

public static class ListMutationMappings
{
    public static CreateListCommand ToBusinessCommand(this CreateListRequest request) =>
        new(request.ListName!);

    public static UpdateListCommand ToBusinessCommand(this UpdateListRequest request) =>
        new(request.ListName!);

    public static AddListWordCommand ToBusinessCommand(this AddListWordRequest request) =>
        new(request.WordId, request.AddMethod!, request.Note);

    public static AddRandomListWordsCommand ToBusinessCommand(this AddRandomListWordsRequest request) =>
        new(request.TopicId, request.Count, request.Method);

    public static UpdateListWordNoteCommand ToBusinessCommand(this UpdateListWordNoteRequest request) =>
        new(request.Note);

    public static AddPersonalTopicWordCommand ToBusinessCommand(
        this AddPersonalTopicWordRequest request) =>
        new(request.WordId, request.Note);

    public static UserListResponse ToResponse(this UserListSummary list) =>
        new(list.ListId, list.ListName, list.WordCount, list.CreatedAt);

    public static ListWordResponse ToResponse(this ListWord word) =>
        new(
            word.WordId,
            word.Word,
            word.PrimaryMeaning,
            word.CorrectCount,
            word.WrongCount,
            word.Note,
            word.AddedAt);

    public static PersonalTopicResponse ToResponse(this PersonalTopic topic) =>
        new(
            topic.TopicId,
            topic.ListId,
            topic.Name,
            topic.NameVi,
            topic.Icon,
            topic.WordCount,
            topic.ContainsWord);

    public static AddRandomListWordsResponse ToResponse(this AddRandomListWordsResult result) =>
        new(result.AddedCount, result.Words.Select(ToResponse).ToArray());
}
