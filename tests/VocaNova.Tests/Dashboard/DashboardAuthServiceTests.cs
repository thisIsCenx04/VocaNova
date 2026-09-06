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

    [Fact]
    public async Task ForgotPasswordAsync_Should_Call_Forgot_Password_Api()
    {
        var handler = new QueueHttpMessageHandler(
            request =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri!.PathAndQuery.Should().Be("/api/auth/forgot-password");
                return JsonResponse("""
                    {"success":true,"data":{"expires_in":300},"message":"Password reset OTP sent successfully.","errors":[]}
                    """);
            },
            async request => (await request.Content!.ReadAsStringAsync()).Should().Contain("\"phone\":\"0912345678\""));
        var service = CreateService(handler);

        var result = await service.ForgotPasswordAsync("0912345678");

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Password reset OTP sent successfully.");
        handler.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Call_Reset_Password_Api_With_Snake_Case_Fields()
    {
        var handler = new QueueHttpMessageHandler(
            request =>
            {
                request.Method.Should().Be(HttpMethod.Post);
                request.RequestUri!.PathAndQuery.Should().Be("/api/auth/reset-password");
                return JsonResponse("""
                    {"success":true,"data":true,"message":"Password reset successfully.","errors":[]}
                    """);
            },
            async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();
                body.Should().Contain("\"phone\":\"0912345678\"");
                body.Should().Contain("\"otp_code\":\"123456\"");
                body.Should().Contain("\"new_password\":\"NewPassword1\"");
            });
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("0912345678", "123456", "NewPassword1");

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Password reset successfully.");
        handler.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Return_Api_Error_Message()
    {
        var handler = new QueueHttpMessageHandler(
            _ => JsonResponse(
                """{"success":false,"data":null,"message":"Invalid or expired OTP.","errors":["Invalid or expired OTP."]}""",
                HttpStatusCode.Unauthorized));
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("0912345678", "000000", "NewPassword1");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Invalid or expired OTP.");
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

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
        private readonly Queue<Func<HttpRequestMessage, Task>> _assertions;

        public QueueHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> response,
            params Func<HttpRequestMessage, Task>[] assertions)
            : this([response], assertions)
        {
        }

        public QueueHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
            : this(responses, [])
        {
        }

        private QueueHttpMessageHandler(
            IEnumerable<Func<HttpRequestMessage, HttpResponseMessage>> responses,
            IEnumerable<Func<HttpRequestMessage, Task>> assertions)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
            _assertions = new Queue<Func<HttpRequestMessage, Task>>(assertions);
        }

        public int PendingCount => _responses.Count;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _responses.Count.Should().BeGreaterThan(0);
            if (_assertions.Count > 0)
            {
                await _assertions.Dequeue().Invoke(request);
            }

            return _responses.Dequeue().Invoke(request);
        }
    }
}
