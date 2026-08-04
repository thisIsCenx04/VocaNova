using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IWordSearchCache
{
    Task<PagedResult<WordSummaryDto>?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    Task SetAsync(
        string cacheKey,
        PagedResult<WordSummaryDto> result,
        CancellationToken cancellationToken = default);
}
