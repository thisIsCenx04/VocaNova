using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using VocaNova.API.Features.Dictionary.BLL.Abstractions;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Infrastructure.ExternalServices.Pexels;

public sealed class PexelsMediaSuggestionProvider : IMediaSuggestionProvider
{
    private const string ProviderName = "pexels";

    private readonly HttpClient _httpClient;
    private readonly PexelsSettings _settings;
    private readonly ILogger<PexelsMediaSuggestionProvider> _logger;

    public PexelsMediaSuggestionProvider(
        HttpClient httpClient,
        IOptions<PexelsSettings> settings,
        ILogger<PexelsMediaSuggestionProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaSuggestionResult>> SearchAsync(
        string query,
        string mediaType,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Pexels API key is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(query, mediaType, limit));
            request.Headers.TryAddWithoutValidation("Authorization", _settings.ApiKey.Trim());
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildFailure(response.StatusCode);
            }

            return mediaType == MediaSuggestionTypes.Video
                ? MapVideos(await response.Content.ReadFromJsonAsync<PexelsVideoSearchResponse>(cancellationToken))
                : MapPhotos(await response.Content.ReadFromJsonAsync<PexelsPhotoSearchResponse>(cancellationToken));
        }
        catch (Exception exception) when (ShouldReportProviderFailure(exception, cancellationToken))
        {
            _logger.LogWarning(exception, "Pexels {MediaType} search failed.", mediaType);
            throw new InvalidOperationException("Pexels media search is unavailable. Please try again later.", exception);
        }
    }

    private string BuildUri(string query, string mediaType, int limit)
    {
        var path = mediaType == MediaSuggestionTypes.Video
            ? "v1/videos/search"
            : "v1/search";
        var queryParams = new Dictionary<string, string?>
        {
            ["query"] = query.Trim(),
            ["per_page"] = Math.Clamp(limit, 1, 20).ToString(CultureInfo.InvariantCulture),
            ["locale"] = string.IsNullOrWhiteSpace(_settings.Locale) ? "en-US" : _settings.Locale.Trim(),
        };
        return QueryHelpers.AddQueryString(path, queryParams);
    }

    private static InvalidOperationException BuildFailure(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new InvalidOperationException("Pexels API key is invalid or unauthorized."),
            HttpStatusCode.TooManyRequests =>
                new InvalidOperationException("Pexels rate limit exceeded. Please try again later."),
            _ => new InvalidOperationException("Pexels media search failed."),
        };

    private static bool ShouldReportProviderFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException
        || exception is System.Text.Json.JsonException
        || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested;

    private static IReadOnlyList<MediaSuggestionResult> MapPhotos(PexelsPhotoSearchResponse? response) =>
        response?.Photos?
            .Where(photo => photo.Id > 0 && !string.IsNullOrWhiteSpace(photo.Url))
            .Select(photo => new MediaSuggestionResult(
                MediaSuggestionTypes.Image,
                ProviderName,
                photo.Id.ToString(CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(photo.Alt) ? $"Photo by {photo.Photographer}" : photo.Alt.Trim(),
                FirstUrl(photo.Src?.Medium, photo.Src?.Large, photo.Src?.Original),
                FirstUrl(photo.Src?.Large2x, photo.Src?.Large, photo.Src?.Original),
                photo.Url!,
                photo.Photographer,
                photo.PhotographerUrl,
                photo.Width,
                photo.Height,
                null))
            .Where(item => !string.IsNullOrWhiteSpace(item.PreviewUrl))
            .ToArray()
        ?? [];

    private static IReadOnlyList<MediaSuggestionResult> MapVideos(PexelsVideoSearchResponse? response) =>
        response?.Videos?
            .Where(video => video.Id > 0 && !string.IsNullOrWhiteSpace(video.Url))
            .Select(video => new MediaSuggestionResult(
                MediaSuggestionTypes.Video,
                ProviderName,
                video.Id.ToString(CultureInfo.InvariantCulture),
                $"Video by {video.User?.Name ?? "Pexels"}",
                FirstUrl(video.Image, video.VideoPictures?.FirstOrDefault()?.Picture),
                BestVideoUrl(video.VideoFiles),
                video.Url!,
                video.User?.Name,
                video.User?.Url,
                video.Width,
                video.Height,
                video.Duration))
            .Where(item => !string.IsNullOrWhiteSpace(item.PreviewUrl))
            .ToArray()
        ?? [];

    private static string FirstUrl(params string?[] urls) =>
        urls.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url))?.Trim() ?? string.Empty;

    private static string? BestVideoUrl(IReadOnlyCollection<PexelsVideoFile>? files) =>
        files?
            .Where(file => string.Equals(file.FileType, "video/mp4", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(file.Link))
            .OrderByDescending(file => file.Width.GetValueOrDefault() * file.Height.GetValueOrDefault())
            .Select(file => file.Link)
            .FirstOrDefault();

    private sealed record PexelsPhotoSearchResponse(
        [property: JsonPropertyName("photos")] IReadOnlyList<PexelsPhoto>? Photos);

    private sealed record PexelsPhoto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("width")] int? Width,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("photographer")] string? Photographer,
        [property: JsonPropertyName("photographer_url")] string? PhotographerUrl,
        [property: JsonPropertyName("alt")] string? Alt,
        [property: JsonPropertyName("src")] PexelsPhotoSource? Src);

    private sealed record PexelsPhotoSource(
        [property: JsonPropertyName("original")] string? Original,
        [property: JsonPropertyName("large2x")] string? Large2x,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("medium")] string? Medium);

    private sealed record PexelsVideoSearchResponse(
        [property: JsonPropertyName("videos")] IReadOnlyList<PexelsVideo>? Videos);

    private sealed record PexelsVideo(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("width")] int? Width,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("duration")] int? Duration,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("image")] string? Image,
        [property: JsonPropertyName("user")] PexelsUser? User,
        [property: JsonPropertyName("video_files")] IReadOnlyCollection<PexelsVideoFile>? VideoFiles,
        [property: JsonPropertyName("video_pictures")] IReadOnlyList<PexelsVideoPicture>? VideoPictures);

    private sealed record PexelsUser(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("url")] string? Url);

    private sealed record PexelsVideoFile(
        [property: JsonPropertyName("width")] int? Width,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("file_type")] string? FileType,
        [property: JsonPropertyName("link")] string? Link);

    private sealed record PexelsVideoPicture(
        [property: JsonPropertyName("picture")] string? Picture);
}
