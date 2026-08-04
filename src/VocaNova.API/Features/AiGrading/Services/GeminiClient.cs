using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.AiGrading.DTOs;

namespace VocaNova.API.Features.AiGrading.Services;

public sealed class GeminiClient : IGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AiGradingSettings _configuredSettings;
    private readonly IAiGradingConfigService? _configService;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(
        HttpClient httpClient,
        IOptions<AiGradingSettings> settings,
        ILogger<GeminiClient> logger,
        IAiGradingConfigService? configService = null)
    {
        _httpClient = httpClient;
        _configuredSettings = settings.Value;
        _logger = logger;
        _configService = configService;
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // Resolved per call, not captured at construction: an admin can change the model,
        // endpoint or key from the dashboard and the next grading request must honour it.
        var settings = _configService is null
            ? _configuredSettings
            : await _configService.GetEffectiveSettingsAsync(cancellationToken);

        return await GenerateContentAsync(prompt, settings, cancellationToken);
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        AiGradingSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = GetModelCandidates(settings);
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || models.Count == 0)
        {
            throw new InvalidOperationException("Gemini configuration is missing.");
        }

        var maxAttempts = Math.Clamp(settings.MaxAttempts, 1, 4);
        var unavailableModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                await DelayBeforeRetryAsync(settings, attempt, cancellationToken);
            }

            foreach (var model in models)
            {
                if (unavailableModels.Contains(model))
                {
                    continue;
                }

                try
                {
                    return await GenerateWithModelAsync(
                        model,
                        prompt,
                        settings,
                        cancellationToken);
                }
                catch (HttpRequestException exception) when (IsModelUnavailable(exception))
                {
                    lastException = exception;
                    unavailableModels.Add(model);
                    _logger.LogWarning(
                        exception,
                        "Gemini model {Model} is unavailable; trying the next configured model.",
                        model);
                }
                catch (HttpRequestException exception) when (IsTransient(exception))
                {
                    lastException = exception;
                    _logger.LogWarning(
                        exception,
                        "Transient Gemini failure for model {Model} on attempt {Attempt}/{MaxAttempts}.",
                        model,
                        attempt,
                        maxAttempts);
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new TimeoutException(
                        $"Gemini model {model} exceeded the per-attempt timeout.",
                        exception);
                    _logger.LogWarning(
                        lastException,
                        "Gemini model {Model} timed out on attempt {Attempt}/{MaxAttempts}.",
                        model,
                        attempt,
                        maxAttempts);
                }
            }

            if (unavailableModels.Count == models.Count)
            {
                break;
            }
        }

        throw new InvalidOperationException(
            "All configured Gemini models failed.",
            lastException);
    }

    private async Task<string> GenerateWithModelAsync(
        string model,
        string prompt,
        AiGradingSettings settings,
        CancellationToken cancellationToken)
    {
        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCts.CancelAfter(TimeSpan.FromSeconds(
            Math.Clamp(settings.AttemptTimeoutSeconds, 1, 15)));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildRequestUri(model, settings));
        request.Headers.Add("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(CreateRequestBody(prompt), options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, attemptCts.Token);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(attemptCts.Token);
        using var json = await JsonDocument.ParseAsync(
            contentStream,
            cancellationToken: attemptCts.Token);

        return ExtractText(json.RootElement);
    }

    /// <summary>
    /// The endpoint is admin-editable, so the URI is built per request instead of relying on
    /// the <see cref="HttpClient"/> base address captured at startup.
    /// </summary>
    private static Uri BuildRequestUri(string model, AiGradingSettings settings)
    {
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new AiGradingSettings().Endpoint
            : settings.Endpoint.Trim().TrimEnd('/');

        return new Uri($"{endpoint}/{NormalizeModelName(model)}:generateContent");
    }

    private static IReadOnlyList<string> GetModelCandidates(AiGradingSettings settings)
    {
        return new[] { settings.Model }
            .Concat(settings.FallbackModels ?? [])
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task DelayBeforeRetryAsync(
        AiGradingSettings settings,
        int attempt,
        CancellationToken cancellationToken)
    {
        var baseDelayMs = Math.Clamp(settings.RetryBaseDelayMs, 0, 5_000);
        if (baseDelayMs == 0)
        {
            return;
        }

        var exponentialDelay = baseDelayMs * (1 << (attempt - 2));
        var jitter = Random.Shared.Next(0, Math.Max(1, baseDelayMs / 2));
        await Task.Delay(exponentialDelay + jitter, cancellationToken);
    }

    private static bool IsModelUnavailable(HttpRequestException exception)
    {
        return exception.StatusCode == HttpStatusCode.NotFound;
    }

    private static bool IsTransient(HttpRequestException exception)
    {
        return exception.StatusCode is null
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static object CreateRequestBody(string prompt)
    {
        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt },
                    },
                },
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json",
            },
        };
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no candidates.");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response has no text parts.");
        }

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("Gemini response text is missing.");
    }

    private static string NormalizeModelName(string model)
    {
        var normalized = model.Trim();
        if (normalized.StartsWith("models/", StringComparison.Ordinal))
        {
            return $"models/{Uri.EscapeDataString(normalized["models/".Length..])}";
        }

        return $"models/{Uri.EscapeDataString(normalized)}";
    }
}
