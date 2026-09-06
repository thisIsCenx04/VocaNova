namespace VocaNova.API.Infrastructure.ExternalServices.Pexels;

public sealed class PexelsSettings
{
    public const string SectionName = "Pexels";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.pexels.com";

    public string Locale { get; set; } = "en-US";

    public int TimeoutSeconds { get; set; } = 10;
}
