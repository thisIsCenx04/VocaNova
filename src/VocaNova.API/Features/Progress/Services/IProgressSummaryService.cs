using VocaNova.API.Common.Results;
using VocaNova.API.Features.Progress.DTOs;

namespace VocaNova.API.Features.Progress.Services;

public interface IProgressSummaryService
{
    Task<Result<ProgressSummaryDto>> GetSummaryAsync(
        uint userId,
        CancellationToken cancellationToken = default);
}
