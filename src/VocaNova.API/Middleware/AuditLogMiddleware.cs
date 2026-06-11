using System.Security.Claims;
using System.Text.Json;
using VocaNova.API.Infrastructure.Auditing;

namespace VocaNova.API.Middleware;

public sealed class AuditLogMiddleware
{
    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    private readonly RequestDelegate _next;
    private readonly IAuditLogQueue _queue;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(
        RequestDelegate next,
        IAuditLogQueue queue,
        ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _queue = queue;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context.Request))
        {
            await _next(context);
            return;
        }

        var requestBody = await ReadRequestBodyAsync(context.Request);

        Exception? thrownException = null;
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            thrownException = exception;
            throw;
        }
        finally
        {
            TryEnqueueAuditLog(context, requestBody, thrownException);
        }
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        return WriteMethods.Contains(request.Method)
            && request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0 || !request.Body.CanRead)
        {
            return null;
        }

        request.EnableBuffering();

        using var reader = new StreamReader(
            request.Body,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return string.IsNullOrWhiteSpace(body) ? null : NormalizeJsonPayload(body);
    }

    private void TryEnqueueAuditLog(HttpContext context, string? requestBody, Exception? exception)
    {
        if (!TryGetUserId(context.User, out var userId))
        {
            _logger.LogWarning(
                "Skipping audit log for {Method} {Path} because no valid user id claim was found.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        var routeInfo = ResolveRouteInfo(context);
        if (string.IsNullOrWhiteSpace(routeInfo.EntityType))
        {
            _logger.LogWarning(
                "Skipping audit log for {Method} {Path} because entity type could not be resolved.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        var payloadAfter = GetStringItem(context, AuditLogHttpContextKeys.PayloadAfter)
            ?? (context.Request.Method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase)
                ? null
                : requestBody);

        var message = new AuditLogMessage(
            UserId: userId,
            Action: ResolveAction(context.Request.Method),
            EntityType: routeInfo.EntityType,
            EntityId: routeInfo.EntityId,
            IpAddress: context.Connection.RemoteIpAddress?.ToString(),
            PayloadBefore: GetStringItem(context, AuditLogHttpContextKeys.PayloadBefore),
            PayloadAfter: payloadAfter,
            CreatedAt: DateTime.UtcNow);

        if (!_queue.TryEnqueue(message))
        {
            _logger.LogError(
                "Failed to enqueue audit log for {Action} {EntityType}/{EntityId}.",
                message.Action,
                message.EntityType,
                message.EntityId);
        }

        if (exception is not null)
        {
            _logger.LogDebug(
                exception,
                "Audit log was queued for a request that later threw an exception.");
        }
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out uint userId)
    {
        var rawUserId = user.FindFirstValue("user_id")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return uint.TryParse(rawUserId, out userId);
    }

    private static (string? EntityType, uint? EntityId) ResolveRouteInfo(HttpContext context)
    {
        var entityType = GetStringItem(context, AuditLogHttpContextKeys.EntityType);
        var entityId = GetUIntItem(context, AuditLogHttpContextKeys.EntityId);

        var pathSegments = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        var adminIndex = Array.FindIndex(pathSegments, segment => segment.Equals("admin", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(entityType) && adminIndex >= 0 && pathSegments.Length > adminIndex + 1)
        {
            entityType = pathSegments[adminIndex + 1];
        }

        if (entityId is null && adminIndex >= 0)
        {
            entityId = pathSegments
                .Skip(adminIndex + 2)
                .Select(segment => uint.TryParse(segment, out var id) ? id : (uint?)null)
                .FirstOrDefault(id => id is not null);
        }

        return (entityType, entityId);
    }

    private static string ResolveAction(string method)
    {
        if (method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            return "Create";
        }

        if (method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
        {
            return "Delete";
        }

        return "Update";
    }

    private static string? GetStringItem(HttpContext context, string key)
    {
        return context.Items.TryGetValue(key, out var value) ? value as string : null;
    }

    private static uint? GetUIntItem(HttpContext context, string key)
    {
        return context.Items.TryGetValue(key, out var value) && value is uint entityId
            ? entityId
            : null;
    }

    private static string NormalizeJsonPayload(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return value;
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { raw = value });
        }
    }
}
