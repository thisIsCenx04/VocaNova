namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record WordVideo(uint VideoId, string Source, string Url, string ThumbnailUrl, string Status);
public sealed record UploadedWordVideo(string PublicId, double DurationSeconds, int Width, int Height, string Format, bool HasVideo);
public sealed record StoredWordVideo(string PublicId, string Url, string ThumbnailUrl);
public sealed record VideoReplacement(WordVideo Video, string? PreviousPublicId);
