using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordSearchCache
{
    Task<PagedCollection<WordSummary>?> GetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string cacheKey,
        PagedCollection<WordSummary> result,
        CancellationToken cancellationToken = default);
}
