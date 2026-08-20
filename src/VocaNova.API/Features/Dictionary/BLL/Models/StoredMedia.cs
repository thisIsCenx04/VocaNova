namespace VocaNova.API.Features.Dictionary.BLL.Models;

public sealed record UploadedContent(
    string FileName,
    string ContentType,
    long Length,
    Stream Content,
    uint OwnerId = 0);

public sealed record StoredMedia(string ObjectKey, string Url);
