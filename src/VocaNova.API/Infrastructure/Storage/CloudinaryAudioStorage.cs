using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryAudioStorage : IAudioStorage
{
    private const int MaxSafeFileNameLength = 80;
    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _client;

    public CloudinaryAudioStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<AudioStorageResult> UploadAsync(
        uint wordId,
        string accent,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var publicId = BuildPublicId(wordId, accent, file.FileName, DateTime.UtcNow, _settings.AudioFolder);
        await using var stream = file.OpenReadStream();
        var result = await _client.Value.UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = publicId,
            Overwrite = true,
            UseFilename = false,
            UniqueFilename = false,
            Folder = null,
        }, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Cloudinary did not return a secure audio URL.");
        }

        return new AudioStorageResult(result.PublicId, url);
    }

    public static string BuildPublicId(uint wordId, string accent, string? fileName, DateTime timestamp,
        string folder = "vocanova/words/audio")
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = "audio";
        var safeName = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray())
            .Trim('-', '.', '_');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "audio";
        if (safeName.Length > MaxSafeFileNameLength) safeName = safeName[..MaxSafeFileNameLength].Trim('-', '.', '_');
        return $"{folder.TrimEnd('/')}/{wordId}/{accent}/{timestamp:yyyyMMddHHmmss}-{safeName}";
    }

    private Cloudinary CreateClient()
    {
        EnsureConfigured();
        return new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret))
        {
            Api = { Secure = true },
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary audio storage is not configured.");
        }
    }
}
