using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Abstractions;

public interface IWordDetailCache
{
    Task<WordDetail?> GetAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        WordDetail word,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(uint wordId, CancellationToken cancellationToken = default);
}
