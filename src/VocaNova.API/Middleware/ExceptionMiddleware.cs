using System.Text.Json;
using VocaNova.API.Common.Responses;

namespace VocaNova.API.Middleware;

public sealed class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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

            // In Development, surface the real exception so it shows up in the
            // client instead of a bare "Internal server error." (aids debugging).
            var errors = _environment.IsDevelopment()
                ? BuildDevErrors(exception)
                : new[] { "Internal server error." };

            var response = ApiResponseFormatter.Error(
                "An unexpected error occurred.",
                errors);

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                response,
                JsonSerializerOptions,
                context.RequestAborted);
        }
    }

    private static string[] BuildDevErrors(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }

        return messages.ToArray();
    }
}
