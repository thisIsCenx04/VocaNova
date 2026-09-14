using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordVideoStorage
{
    Task<UploadedWordVideo> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default);
    Task<StoredWordVideo> PrepareAsync(UploadedWordVideo video, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}
