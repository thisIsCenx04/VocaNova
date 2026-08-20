using VocaNova.API.Common.Models;
using VocaNova.API.Features.Lists.BLL.Models;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Lists.Contracts.Requests;
using VocaNova.API.Features.Lists.Contracts.Responses;

namespace VocaNova.API.Features.Lists.Mappings;

public static class ListQueryMappings
{
    public static ListWordsQuery ToBusinessQuery(this ListWordsRequest request) =>
        new(request.Page, request.Limit);

    public static PersonalTopicQuery ToBusinessQuery(this PersonalTopicListRequest request) =>
        new(request.WordId);

    public static IReadOnlyCollection<UserListResponse> ToResponse(
        this IReadOnlyCollection<UserListSummary> lists) =>
        lists.Select(list => new UserListResponse(
            list.ListId,
            list.ListName,
            list.WordCount,
            list.CreatedAt)).ToArray();

    public static PagedResult<ListWordResponse> ToResponse(this PagedCollection<ListWord> words) =>
        new(
            words.Items.Select(word => new ListWordResponse(
                word.WordId,
                word.Word,
                word.PrimaryMeaning,
                word.CorrectCount,
                word.WrongCount,
                word.Note,
                word.AddedAt)).ToArray(),
            words.Page,
            words.Limit,
            words.TotalItems);

    public static IReadOnlyCollection<PersonalTopicResponse> ToResponse(
        this IReadOnlyCollection<PersonalTopic> topics) =>
        topics.Select(topic => new PersonalTopicResponse(
            topic.TopicId,
            topic.ListId,
            topic.Name,
            topic.NameVi,
            topic.Icon,
            topic.WordCount,
            topic.ContainsWord)).ToArray();
}
