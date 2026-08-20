using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.Dashboard.Models.Api.Dictionary;
using VocaNova.Dashboard.Services.Api;

namespace VocaNova.Tests.Dashboard;

public sealed class DictionaryAdminDashboardCompatibilityTests
{
    [Fact]
    public async Task Dashboard_Client_Should_Consume_Unchanged_Admin_Word_And_Topic_Contracts()
    {
        var handler = new QueueHttpMessageHandler(
            request =>
            {
                request.Method.Should().Be(HttpMethod.Get);
                request.RequestUri!.PathAndQuery.Should().Be(
                    "/api/admin/words?page=2&limit=10&includeDeleted=true&q=run&cefr=A1&status=active&wordType=verb&topicId=3&sortBy=word&sortDirection=asc");
                return JsonResponse("""
                    {"success":true,"data":[{"word_id":7,"word":"run","cefr":"A1","phonetic":"/rÊŒn/","status":"active","image_url":null,"primary_meaning":"cháº¡y","topics":[{"topic_id":3,"name":"Movement","name_vi":"Váº­n Ä‘á»™ng","icon":"run"}],"word_type":"verb"}],"message":"Words loaded successfully.","errors":[],"pagination":{"page":2,"limit":10,"totalItems":11,"totalPages":2}}
                    """);
            },
            request =>
            {
                request.Method.Should().Be(HttpMethod.Get);
                request.RequestUri!.PathAndQuery.Should().Be(
                    "/api/admin/topics?includeDeleted=true&q=sport&status=active");
                return JsonResponse("""
                    {"success":true,"data":[{"topic_id":3,"topic_name":"Sports","topic_name_vi":"Thá»ƒ thao","icon":"ball","status":"active","word_count":4}],"message":"Topics loaded successfully.","errors":[]}
                    """);
            });
        var client = CreateClient(handler);

        var words = await client.GetWordsAsync(new WordListFilter(
            Q: "run", Cefr: "A1", TopicId: 3, Status: "active", IncludeDeleted: true,
            Page: 2, Limit: 10, WordType: "verb", SortBy: "word", SortDirection: "asc"));
        var topics = await client.GetAdminTopicsAsync("sport", "active", true);

        words.Items.Should().ContainSingle().Which.Word.Should().Be("run");
        words.TotalItems.Should().Be(11);
        topics.Should().ContainSingle().Which.TopicName.Should().Be("Sports");
        handler.PendingCount.Should().Be(0);
    }

    private static VocaNovaApiClient CreateClient(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://localhost") },
        NullLogger<VocaNovaApiClient>.Instance);

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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _responses.Count.Should().BeGreaterThan(0);
            return Task.FromResult(_responses.Dequeue().Invoke(request));
        }
    }
}
