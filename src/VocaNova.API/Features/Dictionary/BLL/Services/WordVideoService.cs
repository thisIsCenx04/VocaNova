using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;
using VocaNova.API.Features.Dictionary.BLL.Services.IServices;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public sealed class WordVideoService(IWordVideoRepository repository, IWordVideoStorage storage,
    IWordDetailCache cache, ILogger<WordVideoService> logger) : IWordVideoService
{
    public const long MaxFileBytes = 20 * 1024 * 1024;

    public async Task<DictionaryResult<WordVideo>> SaveAsync(uint wordId, UploadedContent? content, CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length <= 0) return DictionaryResult<WordVideo>.ValidationFailure("Video file is required.");
        if (content.Length > MaxFileBytes) return DictionaryResult<WordVideo>.ValidationFailure("Video file must be 20MB or smaller.");
        if (!string.Equals(Path.GetExtension(content.FileName), ".mp4", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(content.ContentType, "video/mp4", StringComparison.OrdinalIgnoreCase))
            return DictionaryResult<WordVideo>.ValidationFailure("Video must be an MP4 file.");
        if (!await repository.WordExistsAsync(wordId, cancellationToken)) return DictionaryResult<WordVideo>.NotFound("Word not found.");

        UploadedWordVideo? uploaded = null;
        VideoReplacement? replacement;
        try
        {
            uploaded = await storage.UploadAsync(content with { OwnerId = wordId }, cancellationToken);
            if (!uploaded.HasVideo || uploaded.Width <= 0 || uploaded.Height <= 0 || uploaded.Format != "mp4"
                || !double.IsFinite(uploaded.DurationSeconds) || uploaded.DurationSeconds is < 5 or > 15)
            {
                await CleanupAsync(uploaded.PublicId);
                return DictionaryResult<WordVideo>.ValidationFailure("Video must contain a picture and last between 5 and 15 seconds.");
            }
            var prepared = await storage.PrepareAsync(uploaded, cancellationToken);
            if (prepared.PublicId != uploaded.PublicId || prepared.PublicId.Length > 255
                || !ValidUrl(prepared.Url) || !ValidUrl(prepared.ThumbnailUrl))
                throw new InvalidOperationException("Video processing did not return valid media URLs.");
            replacement = await repository.ReplaceAsync(wordId, prepared, cancellationToken);
        }
        catch (Exception exception)
        {
            if (uploaded is not null) await CleanupAsync(uploaded.PublicId);
            if (exception is OperationCanceledException) throw;
            logger.LogWarning(exception, "Word video save failed for word {WordId}.", wordId);
            return DictionaryResult<WordVideo>.ValidationFailure("Unable to save video. Reload the word before retrying.");
        }
        if (replacement is null)
        {
            await CleanupAsync(uploaded.PublicId);
            return DictionaryResult<WordVideo>.NotFound("Word not found.");
        }
        await InvalidateAsync(wordId);
        await CleanupAsync(replacement.PreviousPublicId);
        return DictionaryResult<WordVideo>.Success(replacement.Video);
    }

    public async Task<DictionaryResult<bool>> DeleteAsync(uint wordId, CancellationToken cancellationToken = default)
    {
        if (!await repository.SoftDeleteAsync(wordId, cancellationToken)) return DictionaryResult<bool>.NotFound("Video not found.");
        await InvalidateAsync(wordId);
        return DictionaryResult<bool>.Success(true);
    }

    private static bool ValidUrl(string url) => url.Length <= 500
        && Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host.Length > 0;

    private async Task CleanupAsync(string? publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            // A failed/ cancelled save might have committed. Soft-deleted references count too.
            if (!await repository.IsReferencedAsync(publicId, timeout.Token)) await storage.DeleteAsync(publicId, timeout.Token);
        }
        catch (Exception exception) { logger.LogWarning(exception, "Video cleanup deferred for {PublicId}.", publicId); }
    }

    private async Task InvalidateAsync(uint wordId)
    {
        try { await cache.RemoveAsync(wordId, CancellationToken.None); }
        catch (Exception exception) { logger.LogWarning(exception, "Video was saved but word cache invalidation failed for {WordId}.", wordId); }
    }
}
