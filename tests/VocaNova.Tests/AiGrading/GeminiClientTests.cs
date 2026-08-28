using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.AiGrading.BLL.Models;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;
using VocaNova.API.Features.AiGrading.BLL.Services;
using VocaNova.API.Infrastructure.ExternalServices.Gemini;

namespace VocaNova.Tests.AiGrading;

public sealed class GeminiClientTests
{
    [Fact]
    public async Task GenerateContentAsync_Should_Use_Fallback_Model_When_Primary_Is_Overloaded()
    {
        var handler = new QueueHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.ServiceUnavailable, """{"error":"overloaded"}"""),
            _ => JsonResponse(HttpStatusCode.OK, GeminiResponse("fallback result")));
        var client = CreateClient(handler, fallbackModels: ["fallback-model"]);

        var result = await client.GenerateContentAsync("grade this answer");

        result.Should().Be("fallback result");
        handler.RequestedModels.Should().Equal("primary-model", "fallback-model");
    }

    [Fact]
    public async Task GenerateContentAsync_Should_Retry_Transient_Failures()
    {
        var handler = new QueueHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.TooManyRequests, """{"error":"rate limited"}"""),
            _ => JsonResponse(HttpStatusCode.OK, GeminiResponse("retried result")));
        var client = CreateClient(handler, maxAttempts: 2);

        var result = await client.GenerateContentAsync("grade this answer");

        result.Should().Be("retried result");
        handler.RequestedModels.Should().Equal("primary-model", "primary-model");
    }

    [Fact]
    public async Task GenerateContentAsync_Should_Not_Retry_NonTransient_Failures()
    {
        var handler = new QueueHttpMessageHandler(
            _ => JsonResponse(HttpStatusCode.Forbidden, """{"error":"forbidden"}"""));
        var client = CreateClient(handler, fallbackModels: ["fallback-model"]);

        var act = () => client.GenerateContentAsync("grade this answer");

        await act.Should().ThrowAsync<HttpRequestException>();
        handler.RequestedModels.Should().Equal("primary-model");
    }

    private static GeminiClient CreateClient(
        HttpMessageHandler handler,
        string[]? fallbackModels = null,
        int maxAttempts = 1)
    {
        var settings = Options.Create(new AiGradingSettings
        {
            ApiKey = "test-key",
            Model = "primary-model",
            FallbackModels = fallbackModels ?? [],
            MaxAttempts = maxAttempts,
            RetryBaseDelayMs = 0,
            AttemptTimeoutSeconds = 2,
        });
        return new GeminiClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.test/v1beta/"),
            },
            settings,
            NullLogger<GeminiClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }

    private static string GeminiResponse(string text)
    {
        return $$"""
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      { "text": "{{text}}" }
                    ]
                  }
                }
              ]
            }
            """;
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public QueueHttpMessageHandler(
            params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

        public List<string> RequestedModels { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var segment = request.RequestUri!.Segments[^1];
            RequestedModels.Add(segment[..segment.IndexOf(':')]);
            return Task.FromResult(_responses.Dequeue()(request));
        }
    }
}
