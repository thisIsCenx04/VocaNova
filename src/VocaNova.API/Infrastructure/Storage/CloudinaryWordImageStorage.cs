using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryWordImageStorage : IWordImageStorage
{
    private const int MaxSafeFileNameLength = 80;
    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _client;

    public CloudinaryWordImageStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<StoredMedia> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var publicId = BuildPublicId(content.OwnerId, content.FileName, DateTime.UtcNow, _settings.Folder);
        var result = await _client.Value.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(content.FileName, content.Content), PublicId = publicId,
            Overwrite = true, UseFilename = false, UniqueFilename = false, Folder = null,
        }, cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Cloudinary did not return a secure URL.");
        return new StoredMedia(result.PublicId, url);
    }

    public static string BuildPublicId(uint wordId, string? fileName, DateTime timestamp, string folder = "vocanova/words")
    {
        var name = SanitizeFileNameWithoutExtension(fileName);
        return $"{folder.TrimEnd('/')}/{wordId}/{timestamp:yyyyMMddHHmmss}-{name}";
    }

    public static string SanitizeFileNameWithoutExtension(string? fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = "image";
        var normalized = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray()).Trim('-', '.', '_');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "image";
        if (normalized.Length > MaxSafeFileNameLength) normalized = normalized[..MaxSafeFileNameLength].Trim('-', '.', '_');
        return normalized;
    }

    private Cloudinary CreateClient()
    {
        EnsureConfigured();
        return new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret)) { Api = { Secure = true } };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("Cloudinary image storage is not configured.");
    }
}
