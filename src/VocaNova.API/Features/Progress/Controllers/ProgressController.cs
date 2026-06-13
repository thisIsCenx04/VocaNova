using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VocaNova.API.Common.Extensions;
using VocaNova.API.Common.Results;
using VocaNova.API.Features.Progress.DTOs;
using VocaNova.API.Features.Progress.Services;

namespace VocaNova.API.Features.Progress.Controllers;

[ApiController]
[Authorize]
[Route("api/progress")]
public sealed class ProgressController : ControllerBase
{
    private readonly IProgressSummaryService _progressSummaryService;

    public ProgressController(IProgressSummaryService progressSummaryService)
    {
        _progressSummaryService = progressSummaryService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<ProgressSummaryDto>.Unauthorized("Unauthorized."));
        }

        var result = await _progressSummaryService.GetSummaryAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Progress summary loaded successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
