using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using VocaNova.API.Infrastructure.Sms;

namespace VocaNova.Tests.Auth;

public sealed class SpeedSmsProviderTests
{
    [Fact]
    public async Task SendOtpAsync_Should_Send_Gateway_Request_With_Basic_Authentication()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse(HttpStatusCode.OK, """
                {"status":"success","code":"00","data":{"tranId":"123"}}
                """);
        });
        var provider = CreateProvider(handler);

        await provider.SendOtpAsync("+84 912-345-678", "123456");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should()
            .Be("https://api.speedsms.vn/index.php/sms/send");
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Basic");
        Encoding.UTF8.GetString(
                Convert.FromBase64String(capturedRequest.Headers.Authorization.Parameter!))
            .Should().Be("secret-token:x");

        using var body = JsonDocument.Parse(capturedBody!);
        body.RootElement.GetProperty("to")[0].GetString().Should().Be("0912345678");
        body.RootElement.GetProperty("sms_type").GetInt32().Should().Be(5);
        body.RootElement.GetProperty("sender").GetString().Should().Be("device-123");
        body.RootElement.GetProperty("content").GetString().Should().Contain("123456");
    }

    [Fact]
    public async Task SendOtpAsync_Should_Throw_When_SpeedSms_Rejects_Message()
    {
        var provider = CreateProvider(new StubHandler(_ => Task.FromResult(
            JsonResponse(HttpStatusCode.OK, """
                {"status":"error","code":"105","message":"Phone number invalid"}
                """))));

        var action = () => provider.SendOtpAsync("invalid", "123456");

        await action.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*105*Phone number invalid*");
    }

    private static SpeedSmsProvider CreateProvider(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.speedsms.vn/index.php/"),
        };
        return new SpeedSmsProvider(
            client,
            Options.Create(new SpeedSmsSettings
            {
                Enabled = true,
                AccessToken = "secret-token",
                DeviceId = "device-123",
                SmsType = 5,
            }));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
