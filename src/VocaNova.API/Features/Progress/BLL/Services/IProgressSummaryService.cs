using VocaNova.API.Features.Progress.BLL.Models;

namespace VocaNova.API.Features.Progress.BLL.Services;

public interface IProgressSummaryService
{
    Task<ProgressResult<ProgressSummary>> GetSummaryAsync(
        uint userId,
        CancellationToken cancellationToken = default);
}
