using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VocaNova.API.Common.Models;
using BusinessWordSearchQuery = VocaNova.API.Features.Dictionary.BLL.Models.WordSearchQuery;

namespace VocaNova.Tests.Dictionary;

public class DictionaryControllerContractTests
{
    [Fact]
    public async Task Word_Search_Should_Keep_Message_Json_Fields_And_Data_Pagination()
    {
        var service = new Mock<IWordReadService>();
        service.Setup(instance => instance.SearchAsync(
                new BusinessWordSearchQuery("run", 2, 10, "A1", 3, false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DictionaryResult<PagedCollection<WordSummary>>.Success(
                new PagedCollection<WordSummary>(
                    new[] { new WordSummary(7, "run", "/rʌn/", "A1", "chạy", null) },
                    2,
                    10,
                    11)));
        var controller = new WordsController(service.Object);

        var action = await controller.Search(
            new WordSearchRequest
            {
                Q = "run", Page = 2, Limit = 10, Cefr = "A1", TopicId = 3, IsPhrase = false,
            },
            CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be("Words loaded successfully.");
        root.GetProperty("pagination").ValueKind.Should().Be(JsonValueKind.Null);
        var data = root.GetProperty("data");
        data.GetProperty("page").GetInt32().Should().Be(2);
        data.GetProperty("limit").GetInt32().Should().Be(10);
        data.GetProperty("totalItems").GetInt32().Should().Be(11);
        data.GetProperty("totalPages").GetInt32().Should().Be(2);
        data.GetProperty("items")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal("word_id", "word", "phonetic", "cefr", "primary_meaning", "image_url");
    }

    [Fact]
    public async Task Topics_Should_Keep_Message_And_Public_Json_Fields()
    {
        var service = new Mock<ITopicReadService>();
        service.Setup(instance => instance.GetTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DictionaryResult<IReadOnlyCollection<TopicSummary>>.Success(
                new[] { new TopicSummary(2, "Sports", "Thể thao", "ball", 4) }));
        var controller = new TopicsController(service.Object);

        var action = await controller.GetTopics(CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("message").GetString().Should().Be("Topics loaded successfully.");
        root.GetProperty("data")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal("topic_id", "name", "name_vi", "icon", "word_count");
    }

    [Fact]
    public async Task Word_Not_Found_Should_Map_To_Existing_Error_Envelope()
    {
        var service = new Mock<IWordReadService>();
        service.Setup(instance => instance.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DictionaryResult<WordDetail>.NotFound("Word not found."));
        var controller = new WordsController(service.Object);

        var action = await controller.GetById(99, CancellationToken.None);

        var notFound = action.Should().BeOfType<ObjectResult>().Subject;
        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        using var document = Serialize(notFound.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Word not found.");
        root.GetProperty("errors")[0].GetString().Should().Be("Word not found.");
    }

    private static JsonDocument Serialize(object? value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonDocument.Parse(json);
    }
}
