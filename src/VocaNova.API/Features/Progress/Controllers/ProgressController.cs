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
    private readonly IProgressAnalyticsService _progressAnalyticsService;

    public ProgressController(
        IProgressSummaryService progressSummaryService,
        IProgressAnalyticsService progressAnalyticsService)
    {
        _progressSummaryService = progressSummaryService;
        _progressAnalyticsService = progressAnalyticsService;
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

    [HttpGet("chart")]
    public async Task<IActionResult> GetChart(
        [FromQuery] string? granularity,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<ProgressChartDto>.Unauthorized("Unauthorized."));
        }

        var result = await _progressAnalyticsService.GetChartAsync(
            userId,
            granularity,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Progress chart loaded successfully.");
    }

    [HttpGet("mastery-breakdown")]
    public async Task<IActionResult> GetMasteryBreakdown(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<IReadOnlyCollection<MasteryBreakdownDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _progressAnalyticsService.GetMasteryBreakdownAsync(
            userId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Mastery breakdown loaded successfully.");
    }

    [HttpGet("weakest-words")]
    public async Task<IActionResult> GetWeakestWords(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<IReadOnlyCollection<ProgressWeakestWordDto>>.Unauthorized("Unauthorized."));
        }

        var result = await _progressAnalyticsService.GetWeakestWordsAsync(
            userId,
            limit,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Weakest words loaded successfully.");
    }

    [HttpGet("words/{wordId:uint}")]
    public async Task<IActionResult> GetWordProgress(
        [FromRoute] uint wordId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return this.ErrorResult(Result<WordProgressDetailDto>.Unauthorized("Unauthorized."));
        }

        var result = await _progressAnalyticsService.GetWordProgressAsync(
            userId,
            wordId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ErrorResult(result);
        }

        return this.OkResult(result.Value!, "Word progress loaded successfully.");
    }

    private bool TryGetCurrentUserId(out uint userId)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return uint.TryParse(userIdClaim, out userId);
    }
}
