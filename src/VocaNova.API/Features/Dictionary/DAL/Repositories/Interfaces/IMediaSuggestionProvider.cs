using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IMediaSuggestionProvider
{
    Task<IReadOnlyList<MediaSuggestionResult>> SearchAsync(
        string query,
        string mediaType,
        int limit,
        CancellationToken cancellationToken = default);
}
