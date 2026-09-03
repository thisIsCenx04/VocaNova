using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VocaNova.API.Common.Models;
using BusinessAdminWordQuery = VocaNova.API.Features.Dictionary.BLL.Models.AdminWordQuery;

namespace VocaNova.Tests.Dictionary;

public sealed class DictionaryAdminControllerContractTests
{
    [Fact]
    public async Task Word_List_Should_Preserve_Envelope_Pagination_And_Dashboard_Json_Fields()
    {
        var service = new Mock<IWordAdminService>();
        service.Setup(instance => instance.SearchAsync(
                new BusinessAdminWordQuery("run", "A1", 3, "verb", "active", false, 2, 10, "word", "asc"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DictionaryResult<PagedCollection<AdminWordListItem>>.Success(
                new PagedCollection<AdminWordListItem>(
                    [new AdminWordListItem(7, "run", "A1", "/rÊŒn/", "active", null, "cháº¡y",
                        [new WordTopic(3, "Movement", "Váº­n Ä‘á»™ng", "run")], "verb")],
                    2, 10, 11)));
        var controller = WithHttpContext(new AdminWordsController(service.Object));

        var action = await controller.List(new AdminWordQueryRequest
        {
            Q = "run", Cefr = "A1", TopicId = 3, WordType = "verb", Status = "active",
            Page = 2, Limit = 10, SortBy = "word", SortDirection = "asc",
        }, CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("message").GetString().Should().Be("Words loaded successfully.");
        var pagination = root.GetProperty("pagination");
        pagination.GetProperty("page").GetInt32().Should().Be(2);
        pagination.GetProperty("limit").GetInt32().Should().Be(10);
        pagination.GetProperty("totalItems").GetInt32().Should().Be(11);
        root.GetProperty("data")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal("word_id", "word", "cefr", "phonetic", "status", "image_url",
                "primary_meaning", "topics", "word_type");
    }

    [Theory]
    [InlineData(false, 404, "Sense not found.")]
    [InlineData(true, 200, "Sense deleted successfully.")]
    public async Task Sense_Delete_Should_Preserve_Status_And_Envelope(
        bool success, int expectedStatus, string expectedMessage)
    {
        var service = new Mock<IWordAdminService>();
        service.Setup(instance => instance.SoftDeleteSenseAsync(7, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(success
                ? DictionaryResult<bool>.Success(true)
                : DictionaryResult<bool>.NotFound("Sense not found."));
        var controller = WithHttpContext(new AdminWordsController(service.Object));

        var action = await controller.SoftDeleteSense(7, 9, CancellationToken.None);

        var result = action.Should().BeAssignableTo<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatus);
        using var document = Serialize(result.Value);
        document.RootElement.GetProperty("success").GetBoolean().Should().Be(success);
        document.RootElement.GetProperty("message").GetString().Should().Be(expectedMessage);
    }

    [Fact]
    public async Task Topic_Conflict_Should_Remain_409_With_Existing_Error_Envelope()
    {
        var service = new Mock<ITopicAdminService>();
        service.Setup(instance => instance.CreateAsync(
                new CreateTopicCommand("Sports", null, null, null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DictionaryResult<TopicSummary>.Conflict("Topic already exists."));
        var controller = WithHttpContext(new AdminTopicsController(service.Object));

        var action = await controller.Create(new CreateTopicRequest("Sports", null, null), CancellationToken.None);

        var conflict = action.Should().BeOfType<ObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        using var document = Serialize(conflict.Value);
        document.RootElement.GetProperty("message").GetString().Should().Be("Topic already exists.");
        document.RootElement.GetProperty("errors")[0].GetString().Should().Be("Topic already exists.");
    }

    private static T WithHttpContext<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static JsonDocument Serialize(object? value) => JsonDocument.Parse(
        JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
