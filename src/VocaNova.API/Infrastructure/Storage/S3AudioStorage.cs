using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class S3AudioStorage : IAudioStorage, IDisposable
{
    private const int MaxSafeFileNameLength = 80;

    private readonly AudioStorageSettings _settings;
    private readonly Lazy<IAmazonS3> _client;

    public S3AudioStorage(IOptions<AudioStorageSettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public async Task<AudioStorageResult> UploadAsync(
        uint wordId,
        string accent,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var objectKey = BuildObjectKey(wordId, accent, file.FileName, DateTime.UtcNow);
        await using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = file.ContentType,
        };

        await _client.Value.PutObjectAsync(request, cancellationToken);

        return new AudioStorageResult(objectKey, BuildDeliveryUrl(objectKey));
    }

    public static string BuildObjectKey(
        uint wordId,
        string accent,
        string? fileName,
        DateTime timestamp)
    {
        var safeFileName = SanitizeFileName(fileName);
        return $"words/{wordId}/audio/{accent}/{timestamp:yyyyMMddHHmmss}-{safeFileName}";
    }

    public static string SanitizeFileName(string? fileName)
    {
        var safeName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "audio.mp3";
        }

        var extension = Path.GetExtension(safeName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
        var normalized = new string(nameWithoutExtension
            .Select(character => IsSafeFileNameCharacter(character) ? character : '-')
            .ToArray())
            .Trim('-', '.', '_');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "audio";
        }

        if (normalized.Length > MaxSafeFileNameLength)
        {
            normalized = normalized[..MaxSafeFileNameLength].Trim('-', '.', '_');
        }

        extension = NormalizeExtension(extension);
        return $"{normalized}{extension}";
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }

    private IAmazonS3 CreateClient()
    {
        EnsureConfigured();
        var credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);
        return new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(_settings.Region));
    }

    private string BuildDeliveryUrl(string objectKey)
    {
        var escapedKey = Uri.EscapeDataString(objectKey).Replace("%2F", "/", StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(_settings.CloudFrontBaseUrl))
        {
            return $"{_settings.CloudFrontBaseUrl.TrimEnd('/')}/{escapedKey}";
        }

        return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{escapedKey}";
    }

    private void EnsureConfigured()
    {
        if (!string.Equals(_settings.Provider, "S3", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(_settings.BucketName)
            || string.IsNullOrWhiteSpace(_settings.Region)
            || string.IsNullOrWhiteSpace(_settings.AccessKey)
            || string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new InvalidOperationException("S3 audio storage is not configured.");
        }
    }

    private static bool IsSafeFileNameCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.';
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".mp3";
        }

        extension = extension.ToLowerInvariant();
        return extension is ".mp3" or ".wav" or ".ogg"
            ? extension
            : ".mp3";
    }
}
