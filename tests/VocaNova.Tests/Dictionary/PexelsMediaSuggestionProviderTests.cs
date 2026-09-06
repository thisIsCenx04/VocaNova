using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VocaNova.API.Infrastructure.ExternalServices.Pexels;

namespace VocaNova.Tests.Dictionary;

public sealed class PexelsMediaSuggestionProviderTests
{
    [Fact]
    public async Task SearchAsync_Should_Send_Authorized_Photo_Search_And_Map_Result()
    {
        HttpRequestMessage? captured = null;
        var provider = CreateProvider(new StubHandler(request =>
        {
            captured = request;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """
                {"photos":[{"id":123,"width":1200,"height":800,"url":"https://www.pexels.com/photo/runner-123/","photographer":"Sam","photographer_url":"https://www.pexels.com/@sam","alt":"Runner on track","src":{"medium":"https://images.pexels.com/photos/123/medium.jpeg","large":"https://images.pexels.com/photos/123/large.jpeg","large2x":"https://images.pexels.com/photos/123/large2x.jpeg","original":"https://images.pexels.com/photos/123/original.jpeg"}}]}
                """));
        }));

        var results = await provider.SearchAsync("run", MediaSuggestionTypes.Image, 4);

        captured.Should().NotBeNull();
        captured!.RequestUri!.PathAndQuery.Should().Be("/v1/search?query=run&per_page=4&locale=en-US");
        captured.Headers.GetValues("Authorization").Should().ContainSingle().Which.Should().Be("pexels-key");
        var item = results.Should().ContainSingle().Subject;
        item.MediaType.Should().Be("image");
        item.Provider.Should().Be("pexels");
        item.PreviewUrl.Should().Contain("medium");
        item.FullSizeUrl.Should().Contain("large2x");
        item.CreatorName.Should().Be("Sam");
    }

    [Fact]
    public async Task SearchAsync_Should_Map_Rate_Limit_To_Clear_Error()
    {
        var provider = CreateProvider(new StubHandler(_ => Task.FromResult(
            JsonResponse(HttpStatusCode.TooManyRequests, "{}"))));

        var action = () => provider.SearchAsync("run", MediaSuggestionTypes.Video, 4);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pexels rate limit exceeded. Please try again later.");
    }

    [Fact]
    public async Task SearchAsync_Should_Require_Api_Key()
    {
        var provider = CreateProvider(
            new StubHandler(_ => Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"))),
            apiKey: "");

        var action = () => provider.SearchAsync("run", MediaSuggestionTypes.Image, 4);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pexels API key is not configured.");
    }

    private static PexelsMediaSuggestionProvider CreateProvider(HttpMessageHandler handler, string apiKey = "pexels-key")
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.pexels.com/"),
        };
        return new PexelsMediaSuggestionProvider(
            client,
            Options.Create(new PexelsSettings { ApiKey = apiKey }),
            NullLogger<PexelsMediaSuggestionProvider>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request);
    }
}
