namespace VocaNova.API.Features.AiGrading.BLL.Models;

public sealed class AiGradingConfiguration
{
    public const string SectionName = "AiGrading";

    public string Provider { get; set; } = "Gemini";

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.1-flash-lite";

    public string[] FallbackModels { get; set; } = ["gemini-3.5-flash"];

    public int MaxAttempts { get; set; } = 2;

    public int RetryBaseDelayMs { get; set; } = 400;

    public int AttemptTimeoutSeconds { get; set; } = 6;

    public double PassThreshold { get; set; } = 0.75;
}
