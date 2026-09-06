namespace VocaNova.API.Features.Dictionary.BLL.Models;

public static class MediaSuggestionTypes
{
    public const string Image = "image";
    public const string Video = "video";

    public static bool IsSupported(string? mediaType) =>
        string.Equals(mediaType, Image, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mediaType, Video, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? mediaType) =>
        string.Equals(mediaType, Video, StringComparison.OrdinalIgnoreCase) ? Video : Image;
}

public sealed record MediaSuggestionQuery(
    string? Query,
    string? MediaType,
    int Limit);

public sealed record WordMediaSuggestionContext(
    uint WordId,
    string Word,
    string? PrimaryMeaning,
    string? WordClass,
    IReadOnlyCollection<string> TopicNames);

public sealed record MediaSuggestionResult(
    string MediaType,
    string Provider,
    string ExternalId,
    string Title,
    string PreviewUrl,
    string? FullSizeUrl,
    string SourceUrl,
    string? CreatorName,
    string? CreatorUrl,
    int? Width,
    int? Height,
    int? DurationSeconds);
