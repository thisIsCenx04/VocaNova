using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.API.Features.AiGrading;
using VocaNova.API.Features.AiGrading.Services;

namespace VocaNova.Tests.AiGrading;

public class GeminiAiGradingProviderTests
{
    [Fact]
    public async Task GradeAsync_Should_Parse_Gemini_Json_Response()
    {
        var client = new FakeGeminiClient(
            """
            { "score": 0.82, "explanation": "Câu trả lời gần đúng.", "suggestion": "Dùng từ chính xác hơn." }
            """);
        var provider = CreateProvider(client);

        var result = await provider.GradeAsync(10, 1, "nearly correct", "correct");

        result.IsCorrect.Should().BeTrue();
        result.Score.Should().Be(0.82f);
        result.Explanation.Should().Be("Câu trả lời gần đúng.");
        result.Suggestion.Should().Be("Dùng từ chính xác hơn.");
        client.LastPrompt.Should().Contain("word_id: 10");
        client.LastPrompt.Should().Contain("question_type: 1");
    }

    [Fact]
    public async Task GradeAsync_Should_Parse_Json_CodeFence_Response()
    {
        var client = new FakeGeminiClient(
            """
            ```json
            { "score": 1.0, "explanation": "Đúng.", "suggestion": null }
            ```
            """);
        var provider = CreateProvider(client);

        var result = await provider.GradeAsync(10, 1, "correct", "correct");

        result.IsCorrect.Should().BeTrue();
        result.Score.Should().Be(1f);
        result.Explanation.Should().Be("Đúng.");
        result.Suggestion.Should().BeNull();
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{ \"score\": 1.5, \"explanation\": \"invalid\", \"suggestion\": null }")]
    public async Task GradeAsync_Should_Return_Fallback_When_Response_Invalid(string response)
    {
        var client = new FakeGeminiClient(response);
        var provider = CreateProvider(client);

        var result = await provider.GradeAsync(10, 1, "answer", "expected");

        result.IsCorrect.Should().BeFalse();
        result.Score.Should().Be(0f);
        result.Explanation.Should().Be("AI không khả dụng");
        result.Suggestion.Should().BeNull();
    }

    [Fact]
    public async Task GradeAsync_Should_Return_Fallback_When_Client_Fails()
    {
        var client = new FakeGeminiClient(new InvalidOperationException("Gemini failed."));
        var provider = CreateProvider(client);

        var result = await provider.GradeAsync(10, 1, "answer", "expected");

        result.IsCorrect.Should().BeFalse();
        result.Score.Should().Be(0f);
        result.Explanation.Should().Be("AI không khả dụng");
        result.FromAi.Should().BeFalse();
    }

    [Fact]
    public async Task GradeAsync_Should_Award_Fallback_Credit_On_Exact_Match_When_Client_Fails()
    {
        var client = new FakeGeminiClient(new InvalidOperationException("Gemini failed."));
        var provider = CreateProvider(client);

        var result = await provider.GradeAsync(10, 1, " Correct! ", "correct");

        result.IsCorrect.Should().BeTrue();
        result.Score.Should().Be(1f);
        result.FromAi.Should().BeFalse();
    }

    [Fact]
    public async Task GradeAsync_Should_Rethrow_When_Request_Is_Cancelled()
    {
        var client = new FakeGeminiClient(new OperationCanceledException());
        var provider = CreateProvider(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => provider.GradeAsync(10, 1, "answer", "expected", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static GeminiAiGradingProvider CreateProvider(IGeminiClient client)
    {
        return new GeminiAiGradingProvider(
            client,
            NullLogger<GeminiAiGradingProvider>.Instance);
    }

    private sealed class FakeGeminiClient : IGeminiClient
    {
        private readonly string? _response;
        private readonly Exception? _exception;

        public FakeGeminiClient(string response)
        {
            _response = response;
        }

        public FakeGeminiClient(Exception exception)
        {
            _exception = exception;
        }

        public string? LastPrompt { get; private set; }

        public Task<string> GenerateContentAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_response!);
        }

        public Task<string> GenerateContentAsync(
            string prompt,
            AiGradingSettings settings,
            CancellationToken cancellationToken = default)
        {
            return GenerateContentAsync(prompt, cancellationToken);
        }
    }
}
