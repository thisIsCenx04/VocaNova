using VocaNova.API.Common.Models;
using VocaNova.API.Features.Dictionary.BLL.Models;

namespace VocaNova.API.Features.Dictionary.BLL.Services;

public interface IWordReadService
{
    Task<DictionaryResult<PagedCollection<WordSummary>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<DictionaryResult<WordDetail>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<DictionaryResult<WordDetail>> GetDailyAsync(
        CancellationToken cancellationToken = default);
}
