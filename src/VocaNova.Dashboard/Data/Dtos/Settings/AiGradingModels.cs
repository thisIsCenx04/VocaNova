using System.Text.Json.Serialization;

namespace VocaNova.Dashboard.Data.Dtos.Settings;

// Mirror các DTO cấu hình AI grading của VocaNova.API.
// API key không bao giờ được trả về — chỉ có hint đã che và cờ HasApiKey.

public sealed record AiGradingConfig(
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
    // "env_file" khi giá trị nằm trong .env, "fallback" khi không ghi được file.
    [property: JsonPropertyName("storage")] string Storage,
    [property: JsonPropertyName("can_write_env_file")] bool CanWriteEnvFile,
    [property: JsonPropertyName("supported_providers")] IReadOnlyList<string> SupportedProviders)
{
    public bool IsStoredInEnvFile => string.Equals(Storage, "env_file", StringComparison.Ordinal);
}

public sealed record AiGradingConnectionTest(
    [property: JsonPropertyName("succeeded")] bool Succeeded,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("elapsed_ms")] long ElapsedMs,
    [property: JsonPropertyName("message")] string Message);

public sealed record AiGradingConfigInput(
    string? Provider,
    string? Endpoint,
    string? Model,
    IReadOnlyList<string>? FallbackModels,
    string? ApiKey,
    int? MaxAttempts,
    int? RetryBaseDelayMs,
    int? AttemptTimeoutSeconds,
    double? PassThreshold);
