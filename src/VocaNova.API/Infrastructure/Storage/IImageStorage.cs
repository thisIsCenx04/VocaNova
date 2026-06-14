namespace VocaNova.API.Infrastructure.Storage;

public interface IImageStorage
{
    Task<ImageStorageResult> UploadAsync(
        uint wordId,
        IFormFile file,
        CancellationToken cancellationToken = default);
}
