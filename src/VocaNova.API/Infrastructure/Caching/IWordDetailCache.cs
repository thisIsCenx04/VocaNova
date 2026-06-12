using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Infrastructure.Caching;

public interface IWordDetailCache
{
    Task<WordDetailDto?> GetAsync(uint wordId, CancellationToken cancellationToken = default);

    Task SetAsync(WordDetailDto word, CancellationToken cancellationToken = default);

    Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default);
}
