using System.Text.Json;
using VocaNova.API.Common.Responses;

namespace VocaNova.API.Middleware;

public sealed class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponseFormatter.Error(
                "An unexpected error occurred.",
                new[] { "Internal server error." });

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                response,
                JsonSerializerOptions,
                context.RequestAborted);
        }
    }
}
