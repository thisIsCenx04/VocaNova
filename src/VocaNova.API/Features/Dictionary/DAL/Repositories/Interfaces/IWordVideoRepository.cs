using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordVideoRepository
{
    Task<bool> WordExistsAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<VideoReplacement?> ReplaceAsync(uint wordId, StoredWordVideo video, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(uint wordId, CancellationToken cancellationToken = default);
    Task<bool> IsReferencedAsync(string publicId, CancellationToken cancellationToken = default);
}
