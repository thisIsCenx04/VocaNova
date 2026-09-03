using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VocaNova.API.Common.Models;
using BusinessListWordsQuery = VocaNova.API.Features.Lists.BLL.Models.ListWordsQuery;
using ListsController = VocaNova.API.Features.Lists.Controllers.ListsController;
using PersonalTopicsController = VocaNova.API.Features.Lists.Controllers.PersonalTopicsController;

namespace VocaNova.Tests.Lists;

public class ListControllerContractTests
{
    [Fact]
    public async Task GetLists_Should_Preserve_Envelope_Message_And_Json_Fields()
    {
        var service = new Mock<IListQueryService>();
        service.Setup(item => item.GetListsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<IReadOnlyCollection<UserListSummary>>.Success(
                new[] { new UserListSummary(3, "Travel", 2, new DateTime(2026, 1, 2)) }));
        var controller = CreateListsController(service.Object, "7");

        var action = await controller.GetLists(CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be("Lists loaded successfully.");
        root.GetProperty("data")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal("list_id", "list_name", "word_count", "created_at");
    }

    [Fact]
    public async Task GetWords_Should_Keep_Paged_Object_Inside_Data()
    {
        var service = new Mock<IListQueryService>();
        service.Setup(item => item.GetWordsAsync(
                7,
                3,
                new BusinessListWordsQuery(2, 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<PagedCollection<ListWord>>.Success(
                new PagedCollection<ListWord>(
                    new[] { new ListWord(4, "run", "chay", 3, 1, null, default) },
                    2,
                    10,
                    11)));
        var controller = CreateListsController(service.Object, "7");

        var action = await controller.GetWords(
            3,
            new ListWordsRequest { Page = 2, Limit = 10 },
            CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("pagination").ValueKind.Should().Be(JsonValueKind.Null);
        var data = root.GetProperty("data");
        data.GetProperty("page").GetInt32().Should().Be(2);
        data.GetProperty("limit").GetInt32().Should().Be(10);
        data.GetProperty("totalItems").GetInt32().Should().Be(11);
        data.GetProperty("totalPages").GetInt32().Should().Be(2);
        data.GetProperty("items")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal(
                "word_id", "word", "primary_meaning", "correct_count",
                "wrong_count", "note", "added_at");
    }

    [Fact]
    public async Task GetWords_Should_Preserve_Foreign_List_Forbidden_Status_And_Message()
    {
        var service = new Mock<IListQueryService>();
        service.Setup(item => item.GetWordsAsync(
                7,
                9,
                It.IsAny<BusinessListWordsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<PagedCollection<ListWord>>.Forbidden(
                "You do not have access to this list."));
        var controller = CreateListsController(service.Object, "7");

        var action = await controller.GetWords(9, new ListWordsRequest(), CancellationToken.None);

        AssertError(action, StatusCodes.Status403Forbidden, "You do not have access to this list.");
    }

    [Fact]
    public async Task GetWords_Should_Preserve_Missing_List_NotFound_Status_And_Message()
    {
        var service = new Mock<IListQueryService>();
        service.Setup(item => item.GetWordsAsync(
                7,
                9,
                It.IsAny<BusinessListWordsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<PagedCollection<ListWord>>.NotFound("List not found."));
        var controller = CreateListsController(service.Object, "7");

        var action = await controller.GetWords(9, new ListWordsRequest(), CancellationToken.None);

        AssertError(action, StatusCodes.Status404NotFound, "List not found.");
    }

    [Fact]
    public async Task GetTopics_Should_Preserve_Invalid_Filter_Word_NotFound_Status_And_Message()
    {
        var service = new Mock<IPersonalTopicQueryService>();
        service.Setup(item => item.GetTopicsAsync(
                7,
                new PersonalTopicQuery(99),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<IReadOnlyCollection<PersonalTopic>>.NotFound("Word not found."));
        var controller = CreatePersonalTopicsController(service.Object, "7");

        var action = await controller.GetTopics(
            new PersonalTopicListRequest { WordId = 99 },
            CancellationToken.None);

        AssertError(action, StatusCodes.Status404NotFound, "Word not found.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public async Task Controllers_Should_Return_Unauthorized_When_UserId_Claim_Is_Missing_Or_Invalid(
        string? userId)
    {
        var listService = new Mock<IListQueryService>(MockBehavior.Strict);
        var TopicReadService = new Mock<IPersonalTopicQueryService>(MockBehavior.Strict);

        var listAction = await CreateListsController(listService.Object, userId)
            .GetLists(CancellationToken.None);
        var topicAction = await CreatePersonalTopicsController(TopicReadService.Object, userId)
            .GetTopics(new PersonalTopicListRequest(), CancellationToken.None);

        AssertError(listAction, StatusCodes.Status401Unauthorized, "Unauthorized.");
        AssertError(topicAction, StatusCodes.Status401Unauthorized, "Unauthorized.");
        listService.VerifyNoOtherCalls();
        TopicReadService.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(ResponseContractCases))]
    public void Response_Contracts_Should_Keep_Existing_Json_Names(
        Type responseType,
        string[] expectedNames)
    {
        var names = responseType.GetProperties()
            .Select(property => property
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .Single().Name);
        names.Should().Equal(expectedNames);
    }

    public static TheoryData<Type, string[]> ResponseContractCases =>
        new()
        {
            { typeof(UserListResponse), new[] { "list_id", "list_name", "word_count", "created_at" } },
            { typeof(ListWordResponse), new[] { "word_id", "word", "primary_meaning", "correct_count", "wrong_count", "note", "added_at" } },
            { typeof(PersonalTopicResponse), new[] { "topic_id", "list_id", "name", "name_vi", "icon", "word_count", "contains_word" } },
        };

    private static ListsController CreateListsController(IListQueryService service, string? userId) =>
        SetUser(
            new ListsController(
                service,
                new Mock<IListMutationService>(MockBehavior.Strict).Object),
            userId);

    private static PersonalTopicsController CreatePersonalTopicsController(
        IPersonalTopicQueryService service,
        string? userId) =>
        SetUser(
            new PersonalTopicsController(
                service,
                new Mock<IPersonalTopicMutationService>(MockBehavior.Strict).Object),
            userId);

    private static T SetUser<T>(T controller, string? userId) where T : ControllerBase
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim("user_id", userId) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
            },
        };
        return controller;
    }

    private static void AssertError(IActionResult action, int status, string message)
    {
        var error = action.Should().BeOfType<ObjectResult>().Subject;
        error.StatusCode.Should().Be(status);
        using var document = Serialize(error.Value);
        document.RootElement.GetProperty("message").GetString().Should().Be(message);
        document.RootElement.GetProperty("errors")[0].GetString().Should().Be(message);
    }

    private static JsonDocument Serialize(object? value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
