using VocaNova.API.Features.AiGrading.BLL.Models;

namespace VocaNova.API.Infrastructure.ExternalServices.Gemini;

internal sealed record GeminiSettings(
    string Endpoint,
    string ApiKey,
    string Model,
    IReadOnlyList<string> FallbackModels,
    int MaxAttempts,
    int RetryBaseDelayMs,
    int AttemptTimeoutSeconds)
{
    public static GeminiSettings From(AiGradingConfiguration configuration) => new(
        configuration.Endpoint, configuration.ApiKey, configuration.Model,
        configuration.FallbackModels, configuration.MaxAttempts,
        configuration.RetryBaseDelayMs, configuration.AttemptTimeoutSeconds);
}
