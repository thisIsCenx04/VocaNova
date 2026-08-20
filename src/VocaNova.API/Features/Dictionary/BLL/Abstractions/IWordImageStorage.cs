using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordImageStorage
{
    Task<StoredMedia> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default);
}
