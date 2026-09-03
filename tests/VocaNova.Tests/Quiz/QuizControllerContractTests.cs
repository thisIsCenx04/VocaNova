using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace VocaNova.Tests.Quiz;

public sealed class QuizControllerContractTests
{
    [Fact]
    public async Task Quiz_Create_Should_Preserve_Status_Message_Envelope_And_Json()
    {
        var sessionService = new Mock<IQuizSessionService>();
        var startedAt = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
        var question = new QuizQuestion(4, 5, 1, "fly", "bay", ["bay", "chay"]);
        sessionService.Setup(service => service.CreateSessionAsync(
                7, It.IsAny<CreateQuizSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuizOperationResult<CreatedQuizSession>.Success(
                new CreatedQuizSession(
                    new QuizSession(10, "multiple_choice", "standard", 1, "all",
                        null, null, "random", null, null, null, 10, "in_progress",
                        startedAt, []),
                    question)));
        var controller = CreateQuizController(sessionService: sessionService.Object, userId: "7");

        var action = await controller.Create(
            new CreateSessionRequest("standard", 1, "all", null, null, null,
                "random", null, null, null, "multiple_choice"),
            CancellationToken.None);

        var root = AssertEnvelope(action, 201, true, "Quiz session created successfully.");
        var data = root.GetProperty("data");
        data.EnumerateObject().Select(property => property.Name)
            .Should().Equal("session", "first_question");
        data.GetProperty("session").GetProperty("session_id").GetUInt32().Should().Be(10);
        data.GetProperty("first_question").GetProperty("word_id").GetUInt32().Should().Be(4);
    }

    [Fact]
    public async Task Quiz_Error_And_Unauthorized_Should_Preserve_Status_And_Envelope()
    {
        var submissionService = new Mock<IQuizSubmissionService>();
        submissionService.Setup(service => service.SubmitAnswerAsync(
                7, 10, It.IsAny<SubmitAnswerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QuizOperationResult<QuizAnswer>.Conflict(
                "Quiz session is not in progress."));

        var conflict = await CreateQuizController(
                submissionService: submissionService.Object, userId: "7")
            .SubmitAnswer(10, new SubmitAnswerRequest(4, "bay"), CancellationToken.None);
        AssertEnvelope(conflict, 409, false, "Quiz session is not in progress.");

        var unauthorized = await CreateQuizController(userId: null)
            .GetResult(10, CancellationToken.None);
        AssertEnvelope(unauthorized, 401, false, "Unauthorized.");
    }

    [Fact]
    public async Task Ai_Configuration_Should_Preserve_Status_Message_Envelope_And_Json()
    {
        var configurationService = new Mock<IAiGradingConfigurationService>();
        configurationService.Setup(service => service.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiGradingOperationResult<AiGradingConfigurationView>.Success(
                new AiGradingConfigurationView("Gemini", "https://example.test", "primary",
                    ["fallback"], 2, 400, 6, 0.75, true, "...key", "environment",
                    true, ["Gemini"])));
        var controller = new AdminAiGradingController(
            configurationService.Object,
            new Mock<IAiGradingProvider>(MockBehavior.Strict).Object);

        var action = await controller.GetConfig(CancellationToken.None);

        var root = AssertEnvelope(action, 200, true,
            "AI grading configuration loaded successfully.");
        root.GetProperty("data").EnumerateObject().Select(property => property.Name).Should().Equal(
            "provider", "endpoint", "model", "fallback_models", "max_attempts",
            "retry_base_delay_ms", "attempt_timeout_seconds", "pass_threshold",
            "has_api_key", "api_key_hint", "storage", "can_write_env_file",
            "supported_providers");
    }

    [Theory]
    [MemberData(nameof(JsonContractCases))]
    public void Contracts_Should_Preserve_Explicit_Json_Names(Type type, string[] expectedNames)
    {
        type.GetProperties()
            .Select(property => property
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .Single().Name)
            .Should().Equal(expectedNames);
    }

    public static TheoryData<Type, string[]> JsonContractCases => new()
    {
        { typeof(CreateSessionRequest), new[] { "mode", "question_type", "scope_type", "scope_date_from", "scope_date_to", "topic_ids", "word_order", "word_limit", "time_limit_sec", "lives", "answer_method", "list_id" } },
        { typeof(SubmitAnswerRequest), new[] { "word_id", "user_answer", "list_id" } },
        { typeof(CreateSessionResponse), new[] { "session", "first_question" } },
        { typeof(QuestionResponse), new[] { "word_id", "sense_id", "question_type", "display_content", "expected_answer", "choices" } },
        { typeof(AnswerResponse), new[] { "session_id", "word_id", "is_correct", "expected_answer", "correct_count", "wrong_count", "score", "ai_score", "ai_explanation", "ai_suggestion", "next_question" } },
        { typeof(UpdateAiGradingConfigRequest), new[] { "provider", "endpoint", "model", "fallback_models", "api_key", "max_attempts", "retry_base_delay_ms", "attempt_timeout_seconds", "pass_threshold" } },
        { typeof(AiGradingConnectionTestResponse), new[] { "succeeded", "model", "elapsed_ms", "message" } },
    };

    private static QuizSessionsController CreateQuizController(
        IQuizSessionService? sessionService = null,
        IQuizSubmissionService? submissionService = null,
        IQuizResultService? resultService = null,
        IQuizHistoryService? historyService = null,
        string? userId = "7")
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim("user_id", userId) };
        return new QuizSessionsController(
            sessionService ?? new Mock<IQuizSessionService>(MockBehavior.Strict).Object,
            submissionService ?? new Mock<IQuizSubmissionService>(MockBehavior.Strict).Object,
            resultService ?? new Mock<IQuizResultService>(MockBehavior.Strict).Object,
            historyService ?? new Mock<IQuizHistoryService>(MockBehavior.Strict).Object)
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

    private static JsonElement AssertEnvelope(
        IActionResult action,
        int expectedStatus,
        bool expectedSuccess,
        string expectedMessage)
    {
        var objectResult = action.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            objectResult.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;
        root.GetProperty("success").GetBoolean().Should().Be(expectedSuccess);
        root.GetProperty("message").GetString().Should().Be(expectedMessage);
        return root.Clone();
    }
}
