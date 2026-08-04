namespace VocaNova.API.Infrastructure.Storage;

public interface IImageStorage
{
    Task<ImageStorageResult> UploadAsync(
        uint ownerId,
        IFormFile file,
        string? folder = null,
        CancellationToken cancellationToken = default);
}
