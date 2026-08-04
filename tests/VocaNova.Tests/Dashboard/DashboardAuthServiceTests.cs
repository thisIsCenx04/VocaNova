using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.Dashboard.Services.Auth;

namespace VocaNova.Tests.Dashboard;

public sealed class DashboardAuthServiceTests
{
    [Fact]
    public async Task LoginAsync_Should_Return_User_When_Api_Authenticates_Admin()
    {
        var handler = new QueueHttpMessageHandler(
            request =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri!.PathAndQuery.Should().Be("/api/auth/login");
                return JsonResponse("""
                    {"success":true,"data":{"access_token":"access-token","refresh_token":"refresh-token","expires_in":900,"token_type":"Bearer"},"message":"Logged in successfully.","errors":[]}
                    """);
            },
            request =>
            {
                request.Method.Should().Be(HttpMethod.Get);
                request.RequestUri!.PathAndQuery.Should().Be("/api/auth/me");
                request.Headers.Authorization!.Scheme.Should().Be("Bearer");
                request.Headers.Authorization.Parameter.Should().Be("access-token");
                return JsonResponse("""
                    {"success":true,"data":{"user_id":7,"phone":"0912345678","display_name":"Admin One","avatar_url":null,"role":"admin","status":"active"},"message":"Profile loaded successfully.","errors":[]}
                    """);
            });
        var service = CreateService(handler);

        var result = await service.LoginAsync("0912345678", "Password1");

        result.IsSuccess.Should().BeTrue();
        result.User!.UserId.Should().Be(7);
        result.User.Role.Should().Be("admin");
        result.AccessToken.Should().Be("access-token");
        handler.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_Should_Reject_Non_Admin_And_Revoke_Refresh_Token()
    {
        var handler = new QueueHttpMessageHandler(
            _ => JsonResponse("""
                {"success":true,"data":{"access_token":"access-token","refresh_token":"refresh-token","expires_in":900,"token_type":"Bearer"},"message":"Logged in successfully.","errors":[]}
                """),
            _ => JsonResponse("""
                {"success":true,"data":{"user_id":8,"phone":"0912345679","display_name":"Learner","avatar_url":null,"role":"user","status":"active"},"message":"Profile loaded successfully.","errors":[]}
                """),
            request =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri!.PathAndQuery.Should().Be("/api/auth/logout");
                request.Headers.Authorization!.Parameter.Should().Be("access-token");
                return JsonResponse("""
                    {"success":true,"data":true,"message":"Logged out successfully.","errors":[]}
                    """);
            });
        var service = CreateService(handler);

        var result = await service.LoginAsync("0912345679", "Password1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Dashboard access requires an admin account.");
        handler.PendingCount.Should().Be(0);
    }

    private static DashboardAuthService CreateService(HttpMessageHandler handler)
    {
        return new DashboardAuthService(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost"),
            },
            NullLogger<DashboardAuthService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public QueueHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

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
