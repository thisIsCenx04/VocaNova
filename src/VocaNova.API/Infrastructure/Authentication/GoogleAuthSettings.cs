namespace VocaNova.API.Infrastructure.Authentication;

public sealed class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";

    public string[] ClientIds { get; set; } = Array.Empty<string>();
}
