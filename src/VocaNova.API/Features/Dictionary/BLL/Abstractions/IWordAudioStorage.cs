using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordAudioStorage
{
    Task<StoredMedia> UploadAsync(UploadedContent content, string? accent, CancellationToken cancellationToken = default);
}
