using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.API.Middleware;

namespace VocaNova.Tests.Shared;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Return_500_Response_Without_Exception_Details()
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var middleware = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("Sensitive database failure."),
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var json = await JsonDocument.ParseAsync(responseBody);
        var root = json.RootElement;

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/json");
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
        root.ToString().Should().NotContain("Sensitive database failure.");
        root.ToString().Should().NotContain("InvalidOperationException");
    }
}
