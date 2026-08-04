using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryImageStorage : IImageStorage
{
    private const int MaxSafeFileNameLength = 80;

    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _client;

    public CloudinaryImageStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<ImageStorageResult> UploadAsync(
        uint ownerId,
        IFormFile file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var publicId = BuildPublicId(
            ownerId,
            file.FileName,
            DateTime.UtcNow,
            string.IsNullOrWhiteSpace(folder) ? _settings.Folder : folder);
        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = publicId,
            Overwrite = true,
            UseFilename = false,
            UniqueFilename = false,
            Folder = null,
        };

        var result = await _client.Value.UploadAsync(uploadParams, cancellationToken);
        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Cloudinary did not return a secure URL.");
        }

        return new ImageStorageResult(result.PublicId, url);
    }

    public static string BuildPublicId(
        uint wordId,
        string? fileName,
        DateTime timestamp,
        string folder = "vocanova/words")
    {
        var safeFileName = SanitizeFileNameWithoutExtension(fileName);
        return $"{folder.TrimEnd('/')}/{wordId}/{timestamp:yyyyMMddHHmmss}-{safeFileName}";
    }

    public static string SanitizeFileNameWithoutExtension(string? fileName)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "image";
        }

        var normalized = new string(safeName
            .Select(character => IsSafeFileNameCharacter(character) ? character : '-')
            .ToArray())
            .Trim('-', '.', '_');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "image";
        }

        if (normalized.Length > MaxSafeFileNameLength)
        {
            normalized = normalized[..MaxSafeFileNameLength].Trim('-', '.', '_');
        }

        return normalized;
    }

    private Cloudinary CreateClient()
    {
        EnsureConfigured();
        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        return new Cloudinary(account)
        {
            Api =
            {
                Secure = true,
            },
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary image storage is not configured.");
        }
    }

    private static bool IsSafeFileNameCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.';
    }
}
