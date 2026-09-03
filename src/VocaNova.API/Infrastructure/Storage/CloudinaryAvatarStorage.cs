using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Auth.BLL.Abstractions;
using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryAvatarStorage : IAvatarStorage
{
    private const int MaxSafeFileNameLength = 80;
    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _client;

    public CloudinaryAvatarStorage(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _client = new Lazy<Cloudinary>(CreateClient);
    }

    public async Task<StoredMedia> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var folder = string.IsNullOrWhiteSpace(_settings.AvatarFolder)
            ? _settings.Folder
            : _settings.AvatarFolder;
        var publicId = BuildPublicId(content.OwnerId, content.FileName, DateTime.UtcNow, folder);
        var result = await _client.Value.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(content.FileName, content.Content),
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
            throw new InvalidOperationException("Cloudinary did not return a secure URL.");
        }

        return new StoredMedia(result.PublicId, url);
    }

    public static string BuildPublicId(uint userId, string? fileName, DateTime timestamp, string folder)
    {
        var name = SanitizeFileNameWithoutExtension(fileName);
        return $"{folder.TrimEnd('/')}/{userId}/{timestamp:yyyyMMddHHmmss}-{name}";
    }

    private static string SanitizeFileNameWithoutExtension(string? fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = "avatar";
        var normalized = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray()).Trim('-', '.', '_');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "avatar";
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
        if (string.IsNullOrWhiteSpace(_settings.CloudName)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary image storage is not configured.");
        }
    }
}
