using System.Text.Json.Serialization;

namespace VocaNova.API.Features.AiGrading.Contracts.Requests;

public sealed record UpdateAiGradingConfigRequest(
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("endpoint")] string? Endpoint,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("fallback_models")] IReadOnlyList<string>? FallbackModels,
    [property: JsonPropertyName("api_key")] string? ApiKey,
    [property: JsonPropertyName("max_attempts")] int? MaxAttempts,
    [property: JsonPropertyName("retry_base_delay_ms")] int? RetryBaseDelayMs,
    [property: JsonPropertyName("attempt_timeout_seconds")] int? AttemptTimeoutSeconds,
    [property: JsonPropertyName("pass_threshold")] double? PassThreshold);
