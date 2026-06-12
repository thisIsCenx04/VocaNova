using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Services;

public interface IWordService
{
    Task<Result<PagedResult<WordSummaryDto>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default);
}
