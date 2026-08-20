using System.Text.Json.Serialization;

namespace VocaNova.API.Features.AiGrading.Contracts.Responses;

/// <summary>
/// AI grading configuration as shown to an admin. The API key is never returned — only a
/// masked hint and a flag saying whether one is configured at all.
/// </summary>
public sealed record AiGradingConfigResponse(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("endpoint")] string Endpoint,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("fallback_models")] IReadOnlyList<string> FallbackModels,
    [property: JsonPropertyName("max_attempts")] int MaxAttempts,
    [property: JsonPropertyName("retry_base_delay_ms")] int RetryBaseDelayMs,
    [property: JsonPropertyName("attempt_timeout_seconds")] int AttemptTimeoutSeconds,
    [property: JsonPropertyName("pass_threshold")] double PassThreshold,
    [property: JsonPropertyName("has_api_key")] bool HasApiKey,
    [property: JsonPropertyName("api_key_hint")] string? ApiKeyHint,
    /// <summary><c>env_file</c> when the values live in .env, <c>fallback</c> when the file
    /// could not be written and they are held in the shared store instead.</summary>
    [property: JsonPropertyName("storage")] string Storage,
    [property: JsonPropertyName("can_write_env_file")] bool CanWriteEnvFile,
    [property: JsonPropertyName("supported_providers")] IReadOnlyList<string> SupportedProviders);

/// <summary>
/// Leaving <c>api_key</c> null or blank keeps the key currently in force, so an admin can edit
/// the model or endpoint without having to re-enter the secret.
/// </summary>
