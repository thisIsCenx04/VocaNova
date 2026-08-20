using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryWordAudioStorage : IWordAudioStorage
{
    private const int MaxSafeFileNameLength = 80;
    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _client;

    public CloudinaryWordAudioStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<StoredMedia> UploadAsync(UploadedContent content, string? accent, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var publicId = BuildPublicId(content.OwnerId, accent!, content.FileName, DateTime.UtcNow, _settings.AudioFolder);
        var result = await _client.Value.UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(content.FileName, content.Content), PublicId = publicId,
            Overwrite = true, UseFilename = false, UniqueFilename = false, Folder = null,
        }, cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Cloudinary did not return a secure audio URL.");
        return new StoredMedia(result.PublicId, url);
    }

    public static string BuildPublicId(uint wordId, string accent, string? fileName, DateTime timestamp,
        string folder = "vocanova/words/audio")
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = "audio";
        var safeName = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray()).Trim('-', '.', '_');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "audio";
        if (safeName.Length > MaxSafeFileNameLength) safeName = safeName[..MaxSafeFileNameLength].Trim('-', '.', '_');
        return $"{folder.TrimEnd('/')}/{wordId}/{accent}/{timestamp:yyyyMMddHHmmss}-{safeName}";
    }

    private Cloudinary CreateClient()
    {
        EnsureConfigured();
        return new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret)) { Api = { Secure = true } };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("Cloudinary audio storage is not configured.");
    }
}
