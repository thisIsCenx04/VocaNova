namespace VocaNova.API.Infrastructure.Storage;

public interface IAudioStorage
{
    Task<AudioStorageResult> UploadAsync(
        uint wordId,
        string accent,
        IFormFile file,
        CancellationToken cancellationToken = default);
}
