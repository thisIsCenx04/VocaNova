namespace VocaNova.API.Infrastructure.Storage;

public sealed class AudioStorageSettings
{
    public const string SectionName = "AudioStorage";

    public string Provider { get; set; } = "S3";

    public string? BucketName { get; set; }

    public string? Region { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? CloudFrontBaseUrl { get; set; }
}
