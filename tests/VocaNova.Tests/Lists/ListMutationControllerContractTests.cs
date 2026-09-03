using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ListsController = VocaNova.API.Features.Lists.Controllers.ListsController;
using PersonalTopicsController = VocaNova.API.Features.Lists.Controllers.PersonalTopicsController;

namespace VocaNova.Tests.Lists;

public sealed class ListMutationControllerContractTests
{
    [Fact]
    public async Task List_Mutations_Should_Preserve_Status_Messages_Envelopes_And_Json()
    {
        var service = new Mock<IListMutationService>();
        var now = new DateTime(2026, 8, 20);
        var list = new UserListSummary(3, "Travel", 0, now);
        var word = new ListWord(4, "walk", "di bo", 2, 1, "note", now);
        service.Setup(item => item.CreateAsync(7, new CreateListCommand("Travel"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<UserListSummary>.Success(list));
        service.Setup(item => item.UpdateAsync(7, 3, new UpdateListCommand("Trips"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<UserListSummary>.Success(list with { ListName = "Trips" }));
        service.Setup(item => item.SoftDeleteAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<bool>.Success(true));
        service.Setup(item => item.AddWordAsync(
                7, 3, new AddListWordCommand(4, "manual", "note"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<ListWord>.Success(word));
        service.Setup(item => item.AddRandomWordsAsync(
                7, 3, new AddRandomListWordsCommand(2, 1, "random_topic"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<AddRandomListWordsResult>.Success(
                new AddRandomListWordsResult(1, new[] { word })));
        service.Setup(item => item.RemoveWordAsync(7, 3, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<bool>.Success(true));
        service.Setup(item => item.UpdateWordNoteAsync(
                7, 3, 4, new UpdateListWordNoteCommand("changed"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<ListWord>.Success(word with { Note = "changed" }));
        var controller = CreateListsController(service.Object, "7");

        AssertSuccess(
            await controller.Create(new CreateListRequest("Travel"), CancellationToken.None),
            201,
            "List created successfully.",
            new[] { "list_id", "list_name", "word_count", "created_at" });
        AssertSuccess(
            await controller.Update(3, new UpdateListRequest("Trips"), CancellationToken.None),
            200,
            "List updated successfully.",
            new[] { "list_id", "list_name", "word_count", "created_at" });
        AssertSuccess(
            await controller.SoftDelete(3, CancellationToken.None),
            200,
            "List deleted successfully.");
        AssertSuccess(
            await controller.AddWord(
                3,
                new AddListWordRequest(4, "manual", "note"),
                CancellationToken.None),
            201,
            "Word added to list successfully.",
            new[]
            {
                "word_id", "word", "primary_meaning", "correct_count",
                "wrong_count", "note", "added_at",
            });
        var random = AssertSuccess(
            await controller.AddRandomWords(
                3,
                new AddRandomListWordsRequest(2, 1, "random_topic"),
                CancellationToken.None),
            201,
            "Random words added to list successfully.",
            new[] { "added_count", "words" });
        random.GetProperty("added_count").GetInt32().Should().Be(1);
        AssertSuccess(
            await controller.RemoveWord(3, 4, CancellationToken.None),
            200,
            "Word removed from list successfully.");
        AssertSuccess(
            await controller.UpdateWordNote(
                3,
                4,
                new UpdateListWordNoteRequest("changed"),
                CancellationToken.None),
            200,
            "Word note updated successfully.",
            new[]
            {
                "word_id", "word", "primary_meaning", "correct_count",
                "wrong_count", "note", "added_at",
            });
    }

    [Fact]
    public async Task Personal_Topic_Mutations_Should_Preserve_Status_Messages_And_Envelope()
    {
        var service = new Mock<IPersonalTopicMutationService>();
        service.Setup(item => item.AddWordAsync(
                7, 2, new AddPersonalTopicWordCommand(4, "note"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<PersonalTopic>.Success(
                new PersonalTopic(2, 9, "Travel", "Du lich", null, 1, true)));
        service.Setup(item => item.RemoveWordAsync(7, 2, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<bool>.Success(true));
        var controller = CreatePersonalTopicsController(service.Object, "7");

        AssertSuccess(
            await controller.AddWord(
                2,
                new AddPersonalTopicWordRequest(4, "note"),
                CancellationToken.None),
            201,
            "Word added to personal topic successfully.",
            new[]
            {
                "topic_id", "list_id", "name", "name_vi", "icon",
                "word_count", "contains_word",
            });
        AssertSuccess(
            await controller.RemoveWord(2, 4, CancellationToken.None),
            200,
            "Word removed from personal topic successfully.");
    }

    [Fact]
    public async Task Mutation_Errors_Should_Map_To_Current_Http_Status_And_Message()
    {
        var service = new Mock<IListMutationService>();
        service.Setup(item => item.CreateAsync(7, It.IsAny<CreateListCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<UserListSummary>.Conflict("List name already exists."));
        service.Setup(item => item.UpdateAsync(
                7, 3, It.IsAny<UpdateListCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<UserListSummary>.Forbidden("You do not have access to this list."));
        service.Setup(item => item.RemoveWordAsync(7, 3, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<bool>.NotFound("List word not found."));
        service.Setup(item => item.AddRandomWordsAsync(
                7, 3, It.IsAny<AddRandomListWordsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ListResult<AddRandomListWordsResult>.ValidationFailure("Method is invalid."));
        var controller = CreateListsController(service.Object, "7");

        AssertError(
            await controller.Create(new CreateListRequest("Travel"), CancellationToken.None),
            409,
            "List name already exists.");
        AssertError(
            await controller.Update(3, new UpdateListRequest("Travel"), CancellationToken.None),
            403,
            "You do not have access to this list.");
        AssertError(
            await controller.RemoveWord(3, 4, CancellationToken.None),
            404,
            "List word not found.");
        AssertError(
            await controller.AddRandomWords(
                3,
                new AddRandomListWordsRequest(null, 1, "bad"),
                CancellationToken.None),
            400,
            "Method is invalid.");
    }

    [Fact]
    public async Task Mutations_Should_Return_Unauthorized_Without_Calling_Bll()
    {
        var listService = new Mock<IListMutationService>(MockBehavior.Strict);
        var TopicReadService = new Mock<IPersonalTopicMutationService>(MockBehavior.Strict);

        var listResult = await CreateListsController(listService.Object, null)
            .Create(new CreateListRequest("Travel"), CancellationToken.None);
        var topicResult = await CreatePersonalTopicsController(TopicReadService.Object, "invalid")
            .AddWord(2, new AddPersonalTopicWordRequest(4, null), CancellationToken.None);

        AssertError(listResult, 401, "Unauthorized.");
        AssertError(topicResult, 401, "Unauthorized.");
        listService.VerifyNoOtherCalls();
        TopicReadService.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(ContractCases))]
    public void Mutation_Contracts_Should_Preserve_Json_Names(Type type, string[] expectedNames)
    {
        type.GetProperties()
            .Select(property => property
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .Single().Name)
            .Should().Equal(expectedNames);
    }

    public static TheoryData<Type, string[]> ContractCases =>
        new()
        {
            { typeof(CreateListRequest), new[] { "list_name" } },
            { typeof(UpdateListRequest), new[] { "list_name" } },
            { typeof(AddListWordRequest), new[] { "word_id", "add_method", "note" } },
            { typeof(AddRandomListWordsRequest), new[] { "topic_id", "count", "method" } },
            { typeof(UpdateListWordNoteRequest), new[] { "note" } },
            { typeof(AddPersonalTopicWordRequest), new[] { "word_id", "note" } },
            { typeof(AddRandomListWordsResponse), new[] { "added_count", "words" } },
        };

    private static ListsController CreateListsController(IListMutationService service, string? userId) =>
        SetUser(
            new ListsController(
                new Mock<IListQueryService>(MockBehavior.Strict).Object,
                service),
            userId);

    private static PersonalTopicsController CreatePersonalTopicsController(
        IPersonalTopicMutationService service,
        string? userId) =>
        SetUser(
            new PersonalTopicsController(
                new Mock<IPersonalTopicQueryService>(MockBehavior.Strict).Object,
                service),
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

    private static JsonElement AssertSuccess(
        IActionResult action,
        int expectedStatus,
        string expectedMessage,
        string[]? expectedDataFields = null)
    {
        var objectResult = action.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        using var document = Serialize(objectResult.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        var data = root.GetProperty("data");
        if (expectedDataFields is not null)
        {
            data.EnumerateObject().Select(property => property.Name).Should().Equal(expectedDataFields);
        }

        return data.Clone();
    }

    private static void AssertError(IActionResult action, int expectedStatus, string expectedMessage)
    {
        var objectResult = action.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        using var document = Serialize(objectResult.Value);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("message").GetString().Should().Be(expectedMessage);
        document.RootElement.GetProperty("errors")[0].GetString().Should().Be(expectedMessage);
    }

    private static JsonDocument Serialize(object? value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
