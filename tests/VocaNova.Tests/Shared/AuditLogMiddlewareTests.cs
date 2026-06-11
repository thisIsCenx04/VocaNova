using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using VocaNova.API.Infrastructure.Auditing;
using VocaNova.API.Middleware;

namespace VocaNova.Tests.Shared;

public class AuditLogMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Enqueue_Audit_Log_For_Admin_Put_Request()
    {
        const string payload = """{"word":"hello"}""";
        var queue = new CapturingAuditLogQueue();
        var context = CreateContext(HttpMethods.Put, "/api/admin/words/1", payload, userId: "42");
        string? bodySeenByNext = null;

        var middleware = new AuditLogMiddleware(
            async httpContext =>
            {
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                bodySeenByNext = await reader.ReadToEndAsync();
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
            },
            queue,
            NullLogger<AuditLogMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        bodySeenByNext.Should().Be(payload);
        queue.Messages.Should().ContainSingle();

        var message = queue.Messages.Single();
        message.UserId.Should().Be(42);
        message.Action.Should().Be("Update");
        message.EntityType.Should().Be("words");
        message.EntityId.Should().Be(1);
        message.IpAddress.Should().Be("127.0.0.1");
        message.PayloadBefore.Should().BeNull();
        message.PayloadAfter.Should().Be(payload);
    }

    [Fact]
    public async Task InvokeAsync_Should_Leave_Delete_Payloads_Null()
    {
        var queue = new CapturingAuditLogQueue();
        var context = CreateContext(HttpMethods.Delete, "/api/admin/words/1", body: null, userId: "42");

        var middleware = new AuditLogMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            queue,
            NullLogger<AuditLogMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var message = queue.Messages.Should().ContainSingle().Subject;
        message.Action.Should().Be("Delete");
        message.PayloadBefore.Should().BeNull();
        message.PayloadAfter.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Audit_Log_When_User_Id_Claim_Is_Missing()
    {
        var queue = new CapturingAuditLogQueue();
        var context = CreateContext(HttpMethods.Post, "/api/admin/words", """{"word":"hello"}""", userId: null);

        var middleware = new AuditLogMiddleware(
            _ => Task.CompletedTask,
            queue,
            NullLogger<AuditLogMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        queue.Messages.Should().BeEmpty();
    }

    private static DefaultHttpContext CreateContext(string method, string path, string? body, string? userId)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        if (body is not null)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bodyBytes);
            context.Request.ContentLength = bodyBytes.Length;
            context.Request.ContentType = "application/json";
        }

        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim("user_id", userId) },
                    authenticationType: "Test"));
        }

        return context;
    }

    private sealed class CapturingAuditLogQueue : IAuditLogQueue
    {
        public List<AuditLogMessage> Messages { get; } = new();

        public bool TryEnqueue(AuditLogMessage message)
        {
            Messages.Add(message);
            return true;
        }

        public async IAsyncEnumerable<AuditLogMessage> DequeueAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var message in Messages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return message;
                await Task.Yield();
            }
        }
    }
}
