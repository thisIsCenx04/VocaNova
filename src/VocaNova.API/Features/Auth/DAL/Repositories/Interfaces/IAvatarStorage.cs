using VocaNova.API.Features.Auth.BLL.Models;

namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IAvatarStorage
{
    Task<StoredMedia> UploadAsync(UploadedContent content, CancellationToken cancellationToken = default);
}
