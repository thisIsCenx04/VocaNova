using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace VocaNova.Tests.Progress;

public class ProgressControllerContractTests
{
    [Fact]
    public async Task Summary_Should_Keep_Existing_Envelope_Message_And_Json_Fields()
    {
        var summaryService = new Mock<IProgressSummaryService>();
        summaryService
            .Setup(service => service.GetSummaryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressResult<ProgressSummary>.Success(
                new ProgressSummary(2, 5, 75, 3, 4, 10, 2, 6)));
        var controller = CreateController(
            summaryService.Object,
            Mock.Of<IProgressAnalyticsService>(),
            "7");

        var action = await controller.GetSummary(CancellationToken.None);

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        using var document = Serialize(ok.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("message").GetString().Should().Be(
            "Progress summary loaded successfully.");
        root.GetProperty("data").EnumerateObject().Select(property => property.Name)
            .Should().Equal(
                "current_streak_days",
                "longest_streak_days",
                "accuracy_7d",
                "correct_7d",
                "total_answers_7d",
                "total_words_in_progress",
                "mastered_words",
                "sessions_this_month");
    }

    [Fact]
    public async Task Chart_Should_Map_Validation_Failure_To_Existing_Bad_Request_Envelope()
    {
        var analyticsService = new Mock<IProgressAnalyticsService>();
        analyticsService
            .Setup(service => service.GetChartAsync(
                7,
                new ProgressChartQuery("yearly"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressResult<ProgressChart>.ValidationFailure(
                "Granularity must be daily, weekly, or monthly."));
        var controller = CreateController(
            Mock.Of<IProgressSummaryService>(),
            analyticsService.Object,
            "7");

        var action = await controller.GetChart(
            new ProgressChartRequest { Granularity = "yearly" },
            CancellationToken.None);

        var badRequest = action.Should().BeOfType<ObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        using var document = Serialize(badRequest.Value);
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be(
            "Granularity must be daily, weekly, or monthly.");
        root.GetProperty("errors")[0].GetString().Should().Be(
            "Granularity must be daily, weekly, or monthly.");
    }

    [Fact]
    public async Task Word_Progress_Should_Map_Missing_Record_To_Not_Found()
    {
        var analyticsService = new Mock<IProgressAnalyticsService>();
        analyticsService
            .Setup(service => service.GetWordProgressAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProgressResult<WordProgress>.NotFound("Word progress not found."));
        var controller = CreateController(
            Mock.Of<IProgressSummaryService>(),
            analyticsService.Object,
            "7");

        var action = await controller.GetWordProgress(42, CancellationToken.None);

        var notFound = action.Should().BeOfType<ObjectResult>().Subject;
        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Summary_Should_Return_Unauthorized_When_User_Claim_Is_Missing()
    {
        var summaryService = new Mock<IProgressSummaryService>(MockBehavior.Strict);
        var controller = CreateController(
            summaryService.Object,
            Mock.Of<IProgressAnalyticsService>(),
            null);

        var action = await controller.GetSummary(CancellationToken.None);

        var unauthorized = action.Should().BeOfType<ObjectResult>().Subject;
        unauthorized.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        summaryService.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(ResponseContractCases))]
    public void Response_Contracts_Should_Keep_Existing_Json_Names(
        Type responseType,
        string[] expectedNames)
    {
        var names = responseType.GetProperties()
            .Select(property => property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .Single()
                .Name);

        names.Should().Equal(expectedNames);
    }

    public static TheoryData<Type, string[]> ResponseContractCases =>
        new()
        {
            {
                typeof(ProgressChartPointResponse),
                new[] { "period_start", "period_end", "period_label", "sessions_count", "correct_count", "total_answers", "accuracy" }
            },
            {
                typeof(MasteryBreakdownResponse),
                new[] { "mastery_level", "word_count" }
            },
            {
                typeof(WeakestWordResponse),
                new[] { "word_id", "word", "primary_meaning", "test_count", "correct_count", "wrong_count", "accuracy_rate", "mastery_level", "last_wrong_at", "next_review_at" }
            },
            {
                typeof(WordProgressResponse),
                new[] { "word_id", "word", "primary_meaning", "test_count", "correct_count", "wrong_count", "accuracy_rate", "consecutive_correct", "is_in_wrong_list", "mastery_level", "srs_interval", "ease_factor", "last_tested_at", "last_wrong_at", "next_review_at", "updated_at" }
            },
        };

    private static ProgressController CreateController(
        IProgressSummaryService summaryService,
        IProgressAnalyticsService analyticsService,
        string? userId)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim("user_id", userId) };
        return new ProgressController(summaryService, analyticsService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                },
            },
        };
    }

    private static JsonDocument Serialize(object? value)
    {
        var json = JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonDocument.Parse(json);
    }
}
