using VocaNova.API.Common.Results;
using VocaNova.API.Features.Dictionary.DTOs;

namespace VocaNova.API.Features.Dictionary.Services;

public interface IWordService
{
    Task<Result<PagedResult<WordSummaryDto>>> SearchAsync(
        WordSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> GetByIdAsync(
        uint wordId,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> CreateAsync(
        CreateWordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<WordDetailDto>> UpdateAsync(
        uint wordId,
        UpdateWordRequest request,
        CancellationToken cancellationToken = default);
}
