namespace VocaNova.API.Features.AiGrading;

public sealed class AiGradingSettings
{
    public const string SectionName = "AiGrading";

    public string Provider { get; set; } = "Gemini";

    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash";

    public double PassThreshold { get; set; } = 0.75;
}
