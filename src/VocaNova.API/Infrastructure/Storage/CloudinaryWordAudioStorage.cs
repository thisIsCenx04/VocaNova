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
        var publicId = $"{BuildPublicId(content.OwnerId, accent!, content.FileName, DateTime.UtcNow, _settings.AudioFolder)}-{Guid.NewGuid():N}";
        var result = await _client.Value.UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(content.FileName, content.Content), PublicId = publicId,
            Overwrite = false, UseFilename = false, UniqueFilename = false, Folder = null,
        }, cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
        var url = result.SecureUrl?.ToString();
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Cloudinary did not return a secure audio URL.");
        return new StoredMedia(result.PublicId, url);
    }

    public async Task DeleteOwnedAsync(string url, CancellationToken cancellationToken = default)
    {
        var publicId = GetOwnedPublicId(url, _settings.CloudName ?? "", _settings.AudioFolder);
        if (publicId is null) return;
        EnsureConfigured();
        var result = await _client.Value.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video,
            Invalidate = true,
        }).WaitAsync(cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
    }

    public static string? GetOwnedPublicId(string url, string cloudName, string folder)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || uri.Host != "res.cloudinary.com" || !uri.IsDefaultPort || uri.UserInfo.Length > 0)
            return null;
        var prefix = $"/{cloudName}/video/upload/";
        if (!uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var path = uri.AbsolutePath[prefix.Length..];
        var slash = path.IndexOf('/');
        if (slash < 2 || path[0] != 'v' || !path[1..slash].All(char.IsAsciiDigit)) return null;
        path = path[(slash + 1)..];
        var folderPrefix = folder.Trim('/') + "/";
        if (!path.StartsWith(folderPrefix, StringComparison.Ordinal) || path.Contains('%')) return null;
        var parts = path[folderPrefix.Length..].Split('/');
        if (parts.Length != 3 || !uint.TryParse(parts[0], out _) || parts[1] is not ("uk" or "us")) return null;
        var extension = path.LastIndexOf('.');
        return extension > path.LastIndexOf('/') ? path[..extension] : null;
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
