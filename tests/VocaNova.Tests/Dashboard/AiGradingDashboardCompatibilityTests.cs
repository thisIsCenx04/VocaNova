using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.Dashboard.Models.Api.Settings;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Tests.Dashboard;

public sealed class AiGradingDashboardCompatibilityTests
{
    [Fact]
    public async Task Dashboard_Client_Should_Consume_Unchanged_Ai_Grading_Contracts()
    {
        var handler = new QueueHttpMessageHandler(
            request =>
            {
                AssertRequest(request, HttpMethod.Get, "/api/admin/settings/ai-grading");
                return JsonResponse("""
                    {"success":true,"data":{"provider":"Gemini","endpoint":"https://example.test","model":"primary","fallback_models":["fallback"],"max_attempts":2,"retry_base_delay_ms":400,"attempt_timeout_seconds":6,"pass_threshold":0.75,"has_api_key":true,"api_key_hint":"...key","storage":"env_file","can_write_env_file":true,"supported_providers":["Gemini"]},"message":"AI grading configuration loaded successfully.","errors":[]}
                    """);
            },
            request =>
            {
                AssertRequest(request, HttpMethod.Put, "/api/admin/settings/ai-grading");
                using var reader = new StreamReader(request.Content!.ReadAsStream());
                using var body = JsonDocument.Parse(reader.ReadToEnd());
                body.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal(
                    "provider", "endpoint", "model", "fallback_models", "api_key", "max_attempts",
                    "retry_base_delay_ms", "attempt_timeout_seconds", "pass_threshold");
                return JsonResponse("""{"success":true,"data":{},"message":"AI grading configuration updated successfully.","errors":[]}""");
            },
            request =>
            {
                AssertRequest(request, HttpMethod.Post, "/api/admin/settings/ai-grading/reset");
                return JsonResponse("""{"success":true,"data":{},"message":"AI grading configuration reset to deployment configuration.","errors":[]}""");
            },
            request =>
            {
                AssertRequest(request, HttpMethod.Post, "/api/admin/settings/ai-grading/test");
                return JsonResponse("""
                    {"success":true,"data":{"succeeded":true,"model":"primary","elapsed_ms":12,"message":"Connected."},"message":"AI grading connection test succeeded.","errors":[]}
                    """);
            });
        var client = new VocaNovaApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost") },
            NullLogger<VocaNovaApiClient>.Instance);

        var configuration = await client.GetAiGradingConfigAsync();
        var updated = await client.UpdateAiGradingConfigAsync(new AiGradingConfigInput(
            "Gemini", "https://example.test", "primary", ["fallback"], "secret",
            2, 400, 6, 0.75));
        var reset = await client.ResetAiGradingConfigAsync();
        var connection = await client.TestAiGradingConnectionAsync();

        configuration.Should().NotBeNull();
        configuration!.FallbackModels.Should().Equal("fallback");
        configuration.IsStoredInEnvFile.Should().BeTrue();
        updated.IsSuccess.Should().BeTrue();
        reset.IsSuccess.Should().BeTrue();
        connection.Should().Be(new AiGradingConnectionTest(true, "primary", 12, "Connected."));
        handler.PendingCount.Should().Be(0);
    }

    private static void AssertRequest(HttpRequestMessage request, HttpMethod method, string path)
    {
        request.Method.Should().Be(method);
        request.RequestUri!.PathAndQuery.Should().Be(path);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public QueueHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) =>
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);

        public int PendingCount => _responses.Count;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _responses.Count.Should().BeGreaterThan(0);
            return Task.FromResult(_responses.Dequeue().Invoke(request));
        }
    }
}
