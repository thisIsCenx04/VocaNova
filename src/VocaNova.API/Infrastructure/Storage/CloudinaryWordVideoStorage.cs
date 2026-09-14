using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Infrastructure.Storage;

public sealed class CloudinaryWordVideoStorage(IOptions<CloudinarySettings> options,
    ILogger<CloudinaryWordVideoStorage> logger) : IWordVideoStorage
{
    private const string Folder = "vocanova/words/video";
    private readonly Lazy<Cloudinary> _client = new(() =>
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ApiSecret))
            throw new InvalidOperationException("Cloudinary video storage is not configured.");
        return new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret)) { Api = { Secure = true } };
    });

    public async Task<UploadedWordVideo> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default)
    {
        var client = _client.Value;
        var publicId = $"{Folder}/{content.OwnerId}/{Guid.NewGuid():N}";
        try
        {
            var result = await client.UploadAsync(new VideoUploadParams
            {
                File = new FileDescription(content.FileName, content.Content), PublicId = publicId,
                Overwrite = false, UseFilename = false, UniqueFilename = false,
            }, cancellationToken);
            if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
            if (result.PublicId != publicId) throw new InvalidOperationException("Unexpected video asset identifier.");
            return new UploadedWordVideo(publicId, result.Duration, result.Width, result.Height, result.Format, result.Video is not null);
        }
        catch
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await DeleteAsync(publicId, timeout.Token); }
            catch (Exception exception) { logger.LogWarning(exception, "Incomplete video upload cleanup deferred for {PublicId}.", publicId); }
            throw;
        }
    }

    public async Task<StoredWordVideo> PrepareAsync(UploadedWordVideo video, CancellationToken cancellationToken = default)
    {
        var landscape = video.Width >= video.Height;
        var result = await _client.Value.ExplicitAsync(new ExplicitParams(video.PublicId)
        {
            ResourceType = ResourceType.Video,
            Type = "upload",
            EagerAsync = false,
            EagerTransforms = new List<Transformation>
            {
                new Transformation().Width(landscape ? 1280 : 720).Height(landscape ? 720 : 1280)
                    .Crop("limit").VideoCodec("h264").AudioCodec("aac").FetchFormat("mp4"),
                new Transformation().Width(640).Height(640).Crop("limit").StartOffset("0").FetchFormat("jpg"),
            },
        }, cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
        var playback = result.Eager?.FirstOrDefault(e => e.Format == "mp4");
        var thumbnail = result.Eager?.FirstOrDefault(e => e.Format == "jpg");
        if (playback?.SecureUrl is null || thumbnail?.SecureUrl is null)
            throw new InvalidOperationException("Video processing is incomplete.");
        return new StoredWordVideo(video.PublicId, playback.SecureUrl.ToString(), thumbnail.SecureUrl.ToString());
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (!publicId.StartsWith(Folder + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Video asset is outside the managed folder.");
        var result = await _client.Value.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video, Invalidate = true,
        }).WaitAsync(cancellationToken);
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
    }
}
